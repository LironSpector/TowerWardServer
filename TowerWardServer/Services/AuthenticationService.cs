using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using BCrypt.Net; // For password verify
using DTOs;
using Models;
using Repositories;
using Settings;

namespace Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IUserRepository _userRepository;
        private readonly IAuthenticationRepository _authRepo;
        private readonly JwtSettings _jwtSettings;

        public AuthenticationService(
            IUserRepository userRepository,
            IAuthenticationRepository authRepo,
            IOptions<JwtSettings> jwtSettings)
        {
            _userRepository = userRepository;
            _authRepo = authRepo;
            _jwtSettings = jwtSettings.Value;
        }

        /// <summary>
        /// Logs in via username/password. 
        /// Returns null if invalid credentials.
        /// </summary>
        public async Task<AuthResponseDTO> LoginAsync(string username, string password)
        {
            // 1) Get user by username
            var user = await _userRepository.GetByUsernameAsync(username);
            if (user == null) return null;

            // 2) Check password with BCrypt
            bool valid = BCrypt.Net.BCrypt.Verify(password, user.Password);
            if (!valid) return null;

            // 3) Generate tokens
            var (accessToken, accessExpires) = GenerateAccessToken(user.UserId);
            var (refreshToken, refreshExpires) = GenerateRefreshToken();

            // 4) Save refresh token in DB
            var authRecord = new Authentication
            {
                UserId = user.UserId,
                RefreshToken = refreshToken,
                ExpiryTime = refreshExpires,
                CreatedAt = DateTime.UtcNow
            };
            await _authRepo.AddAsync(authRecord);

            // 5) Return tokens
            return new AuthResponseDTO
            {
                AccessToken = accessToken,
                AccessTokenExpiry = accessExpires,
                RefreshToken = refreshToken,
                RefreshTokenExpiry = refreshExpires
            };
        }

        /// <summary>
        /// Uses the existing refresh token to generate a new access token.
        /// If the refresh token is invalid or expired, returns null.
        /// </summary>
        public async Task<AuthResponseDTO> RefreshAsync(string refreshToken)
        {
            // 1) Lookup refresh token record
            var authRecord = await _authRepo.GetByRefreshTokenAsync(refreshToken);
            if (authRecord == null) return null;

            // 2) Check expiry
            if (authRecord.ExpiryTime < DateTime.UtcNow)
            {
                // expired => remove from DB
                await _authRepo.DeleteAsync(authRecord);
                return null;
            }

            var userId = authRecord.UserId;

            // 3) Generate new tokens
            var (accessToken, accessExpires) = GenerateAccessToken(userId);
            var (newRefreshToken, newRefreshExpires) = GenerateRefreshToken();

            // 4) Update DB record
            authRecord.RefreshToken = newRefreshToken;
            authRecord.ExpiryTime = newRefreshExpires;
            authRecord.CreatedAt = DateTime.UtcNow;
            await _authRepo.UpdateAsync(authRecord);

            // 5) Return new tokens
            return new AuthResponseDTO
            {
                AccessToken = accessToken,
                AccessTokenExpiry = accessExpires,
                RefreshToken = newRefreshToken,
                RefreshTokenExpiry = newRefreshExpires
            };
        }

        /// <summary>
        /// Revoke all refresh tokens for a user. 
        /// Typically used for a "global logout" or an admin action.
        /// </summary>
        public async Task RevokeAllAsync(int userId)
        {
            await _authRepo.RevokeAllTokensForUserAsync(userId);
        }

        // -----------------------------------------------------
        // HELPER METHODS
        // -----------------------------------------------------

        private (string token, DateTime expiry) GenerateAccessToken(int userId)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                // "sub" is a standard claim name for the subject (user ID)
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var expiry = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: expiry,
                signingCredentials: creds);

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);
            return (jwt, expiry);
        }

        private (string token, DateTime expiry) GenerateRefreshToken()
        {
            // Could store as a GUID or a random string
            var refreshToken = Guid.NewGuid().ToString("N");
            var expiry = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);

            return (refreshToken, expiry);
        }
    }
}

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using BCrypt.Net; // For password verify
using DTOs;
using Models;
using Repositories;
using Settings;

namespace Services
{
    /// <summary>
    /// Handles user authentication: login (password → JWT + refresh token),
    /// refresh of tokens, token validation, and revocation of all tokens.
    /// </summary>
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IUserRepository _userRepository;
        private readonly IAuthenticationRepository _authRepo;
        private readonly JwtSettings _jwtSettings;

        /// <summary>
        /// Constructs the service with required repositories and JWT settings.
        /// </summary>
        public AuthenticationService(
            IUserRepository userRepository,
            IAuthenticationRepository authRepo,
            Microsoft.Extensions.Options.IOptions<JwtSettings> jwtSettings)
        {
            _userRepository = userRepository;
            _authRepo = authRepo;
            _jwtSettings = jwtSettings.Value;
        }

        /// <summary>
        /// Validates the provided username/password, issues a new access token and refresh token,
        /// persists the refresh token to the database, and returns both tokens.
        /// </summary>
        /// <param name="username">The user's login name.</param>
        /// <param name="password">The raw password to verify.</param>
        /// <returns>
        /// An <see cref="AuthResponseDTO"/> containing the tokens and expiry times,
        /// or <c>null</c> if credentials are invalid.
        /// </returns>
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
                RefreshTokenExpiry = refreshExpires,
                UserId = user.UserId
            };
        }

        /// <summary>
        /// Exchanges a valid, unexpired refresh token for a new access token (and refresh token).
        /// </summary>
        /// <param name="refreshToken">The existing refresh token to renew.</param>
        /// <returns>
        /// A new <see cref="AuthResponseDTO"/> if the token was valid; otherwise <c>null</c>.
        /// </returns>
        public async Task<AuthResponseDTO> RefreshAsync(string refreshToken)
        {
            // 1) Lookup refresh token record
            var authRecord = await _authRepo.GetByRefreshTokenAsync(refreshToken);
            if (authRecord == null) return null;

            // 2) Check expiry
            if (authRecord.ExpiryTime < DateTime.UtcNow)
            {
                Console.WriteLine("expired => remove from DB");
                // expired => remove from DB
                await _authRepo.DeleteAsync(authRecord);
                return null;
            }

            int userId = authRecord.UserId;

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
                RefreshTokenExpiry = newRefreshExpires,
                UserId = userId
            };
        }

        /// <summary>
        /// Deletes all refresh tokens associated with the given user.
        /// </summary>
        /// <param name="userId">The user whose tokens should be revoked.</param>
        public async Task RevokeAllAsync(int userId)
        {
            await _authRepo.RevokeAllTokensForUserAsync(userId);
        }

        /// <summary>
        /// Validates a JWT access token and extracts the user ID ("sub" claim).
        /// </summary>
        /// <param name="token">The JWT string to validate.</param>
        /// <returns>
        /// A tuple where <c>IsValid</c> is <c>true</c> if the token signature and expiry are valid,
        /// and <c>UserId</c> contains the parsed subject claim; otherwise <c>(false, 0)</c>.
        /// </returns>
        public async Task<(bool IsValid, int UserId)> ValidateTokenAsync(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);

            try
            {
                var parameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ClockSkew = TimeSpan.Zero
                };

                Console.WriteLine("Console 1");
                // Validate the token
                var principal = handler.ValidateToken(token, parameters, out var validatedToken);

                // Check the token is a proper JWT
                if (!(validatedToken is JwtSecurityToken jwtToken)
                    || !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    return (false, 0);
                }

                var idClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
                if (idClaim == null || !int.TryParse(idClaim.Value, out int userId))
                {
                    return (false, 0);
                }

                return (true, userId);
            }
            catch
            {
                // If anything fails (invalid signature, expired, etc.), return invalid
                return (false, 0);
            }
        }

        /// <summary>
        /// Generates a signed JWT access token containing the "sub" claim for the user.
        /// </summary>
        private (string token, DateTime expiry) GenerateAccessToken(int userId)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var expiry = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);

            var jwt = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: expiry,
                signingCredentials: creds);

            return (new JwtSecurityTokenHandler().WriteToken(jwt), expiry);
        }

        /// <summary>
        /// Creates a new opaque refresh token as a GUID string and sets its expiry.
        /// </summary>
        private (string token, DateTime expiry) GenerateRefreshToken()
        {
            var refreshToken = Guid.NewGuid().ToString("N");
            var expiry = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);
            return (refreshToken, expiry);
        }
    }
}

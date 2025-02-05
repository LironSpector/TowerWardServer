// ------------ New AuthenticationService - after JWT validation in server ------------
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
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IUserRepository _userRepository;
        private readonly IAuthenticationRepository _authRepo;
        private readonly JwtSettings _jwtSettings;

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
        /// Logs in via username/password. Returns null if invalid credentials.
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
                // Might also add "UserId = user.UserId" so the caller knows who this token belongs to
                AccessToken = accessToken,
                AccessTokenExpiry = accessExpires,
                RefreshToken = refreshToken,
                RefreshTokenExpiry = refreshExpires,

                // Optional: If you want the client to see userId:
                UserId = user.UserId
            };
        }

        /// <summary>
        /// Uses the existing refresh token to generate a new access token.
        /// If the refresh token is invalid or expired, returns null.
        /// </summary>
        public async Task<AuthResponseDTO> RefreshAsync(string refreshToken)
        {
            Console.WriteLine("Check Num 0");
            // 1) Lookup refresh token record
            var authRecord = await _authRepo.GetByRefreshTokenAsync(refreshToken);
            if (authRecord == null) return null;

            Console.WriteLine("Check Num 1");
            // 2) Check expiry
            if (authRecord.ExpiryTime < DateTime.UtcNow)
            {
                Console.WriteLine("expired => remove from DB");
                // expired => remove from DB
                await _authRepo.DeleteAsync(authRecord);
                return null;
            }

            var userId = authRecord.UserId;
            Console.WriteLine("Check Num 2, userId: " + userId);

            // 3) Generate new tokens
            var (accessToken, accessExpires) = GenerateAccessToken(userId);
            Console.WriteLine("Check Num 3");
            var (newRefreshToken, newRefreshExpires) = GenerateRefreshToken();
            Console.WriteLine("Check Num 4");

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
                // optionally store userId if you want
                UserId = userId
            };
        }

        /// <summary>
        /// Revoke all refresh tokens for a user. Typically used for "global logout" or admin action.
        /// </summary>
        public async Task RevokeAllAsync(int userId)
        {
            await _authRepo.RevokeAllTokensForUserAsync(userId);
        }

        // -----------------------------------------------------------------
        // NEW: VALIDATE TOKEN ASYNC
        // -----------------------------------------------------------------

        /// <summary>
        /// Validates the given JWT access token. 
        /// Returns (IsValid=false, UserId=0) if invalid or expired.
        /// Otherwise, returns (IsValid=true, UserId=[extracted from 'sub' claim]).
        /// </summary>
        public async Task<(bool IsValid, int UserId)> ValidateTokenAsync(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);

            try
            {
                // Define how we validate the token
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
                var principal = tokenHandler.ValidateToken(token, parameters, out var validatedToken);
                Console.WriteLine("Console 2");

                // (Optional) Check the token is a proper JWT, etc.
                if (!(validatedToken is JwtSecurityToken jwtToken)
                    || !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    return (false, 0);
                }
                Console.WriteLine("Console 3");

                // - Extract the user id from "sub" claim -

                //The line below doesn't work, because by default, "sub" is mapped to "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier" (the
                //same as ClaimTypes.NameIdentifier). If I call FindFirst(ClaimTypes.NameIdentifier), I will find the value of "sub".
                //var userIdClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub);

                var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier); //This line is the correct one
                if (userIdClaim == null)
                {
                    return (false, 0);
                }
                Console.WriteLine("Console 4");
                if (!int.TryParse(userIdClaim.Value, out int userId))
                {
                    return (false, 0);
                }

                // (Optional) I could do further checks, e.g. see if user is banned

                return (true, userId);
            }
            catch
            {
                // If anything fails (invalid signature, expired, etc.), return invalid
                return (false, 0);
            }
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
            var refreshToken = Guid.NewGuid().ToString("N");
            var expiry = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);

            return (refreshToken, expiry);
        }
    }
}









// ------------ Previous AuthenticationService - before JWT validation in server ------------
//using System.IdentityModel.Tokens.Jwt;
//using System.Security.Claims;
//using System.Text;
//using Microsoft.Extensions.Options;
//using Microsoft.IdentityModel.Tokens;
//using BCrypt.Net; // For password verify
//using DTOs;
//using Models;
//using Repositories;
//using Settings;

//namespace Services
//{
//    public class AuthenticationService : IAuthenticationService
//    {
//        private readonly IUserRepository _userRepository;
//        private readonly IAuthenticationRepository _authRepo;
//        private readonly JwtSettings _jwtSettings;

//        public AuthenticationService(
//            IUserRepository userRepository,
//            IAuthenticationRepository authRepo,
//            IOptions<JwtSettings> jwtSettings)
//        {
//            _userRepository = userRepository;
//            _authRepo = authRepo;
//            _jwtSettings = jwtSettings.Value;
//        }

//        /// <summary>
//        /// Logs in via username/password. 
//        /// Returns null if invalid credentials.
//        /// </summary>
//        public async Task<AuthResponseDTO> LoginAsync(string username, string password)
//        {
//            // 1) Get user by username
//            var user = await _userRepository.GetByUsernameAsync(username);
//            Console.WriteLine("EEE: ", user == null);
//            if (user == null) return null;

//            Console.WriteLine("FFF");

//            // 2) Check password with BCrypt
//            bool valid = BCrypt.Net.BCrypt.Verify(password, user.Password);
//            Console.WriteLine("GGG");
//            if (!valid) return null;

//            Console.WriteLine("HHH");

//            // 3) Generate tokens
//            var (accessToken, accessExpires) = GenerateAccessToken(user.UserId);
//            Console.WriteLine("III");
//            var (refreshToken, refreshExpires) = GenerateRefreshToken();
//            Console.WriteLine("JJJ");

//            // 4) Save refresh token in DB
//            var authRecord = new Authentication
//            {
//                UserId = user.UserId,
//                RefreshToken = refreshToken,
//                ExpiryTime = refreshExpires,
//                CreatedAt = DateTime.UtcNow
//            };
//            Console.WriteLine("KKK");
//            await _authRepo.AddAsync(authRecord);
//            Console.WriteLine("LLL");

//            // 5) Return tokens
//            return new AuthResponseDTO
//            {
//                AccessToken = accessToken,
//                AccessTokenExpiry = accessExpires,
//                RefreshToken = refreshToken,
//                RefreshTokenExpiry = refreshExpires
//            };
//        }

//        /// <summary>
//        /// Uses the existing refresh token to generate a new access token.
//        /// If the refresh token is invalid or expired, returns null.
//        /// </summary>
//        public async Task<AuthResponseDTO> RefreshAsync(string refreshToken)
//        {
//            // 1) Lookup refresh token record
//            var authRecord = await _authRepo.GetByRefreshTokenAsync(refreshToken);
//            if (authRecord == null) return null;

//            // 2) Check expiry
//            if (authRecord.ExpiryTime < DateTime.UtcNow)
//            {
//                // expired => remove from DB
//                await _authRepo.DeleteAsync(authRecord);
//                return null;
//            }

//            var userId = authRecord.UserId;

//            // 3) Generate new tokens
//            var (accessToken, accessExpires) = GenerateAccessToken(userId);
//            var (newRefreshToken, newRefreshExpires) = GenerateRefreshToken();

//            // 4) Update DB record
//            authRecord.RefreshToken = newRefreshToken;
//            authRecord.ExpiryTime = newRefreshExpires;
//            authRecord.CreatedAt = DateTime.UtcNow;
//            await _authRepo.UpdateAsync(authRecord);

//            // 5) Return new tokens
//            return new AuthResponseDTO
//            {
//                AccessToken = accessToken,
//                AccessTokenExpiry = accessExpires,
//                RefreshToken = newRefreshToken,
//                RefreshTokenExpiry = newRefreshExpires
//            };
//        }

//        /// <summary>
//        /// Revoke all refresh tokens for a user. 
//        /// Typically used for a "global logout" or an admin action.
//        /// </summary>
//        public async Task RevokeAllAsync(int userId)
//        {
//            await _authRepo.RevokeAllTokensForUserAsync(userId);
//        }

//        // -----------------------------------------------------
//        // HELPER METHODS
//        // -----------------------------------------------------

//        private (string token, DateTime expiry) GenerateAccessToken(int userId)
//        {
//            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
//            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

//            var claims = new List<Claim>
//            {
//                // "sub" is a standard claim name for the subject (user ID)
//                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
//                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
//            };

//            var expiry = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);

//            var token = new JwtSecurityToken(
//                issuer: _jwtSettings.Issuer,
//                audience: _jwtSettings.Audience,
//                claims: claims,
//                expires: expiry,
//                signingCredentials: creds);

//            var jwt = new JwtSecurityTokenHandler().WriteToken(token);
//            return (jwt, expiry);
//        }

//        private (string token, DateTime expiry) GenerateRefreshToken()
//        {
//            // Could store as a GUID or a random string
//            var refreshToken = Guid.NewGuid().ToString("N");
//            var expiry = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);

//            return (refreshToken, expiry);
//        }
//    }
//}

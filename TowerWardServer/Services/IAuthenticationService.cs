using DTOs;

namespace Services
{
    public interface IAuthenticationService
    {
        /// <summary>
        /// Logs in a user with username + password, returning an AuthResponseDTO if successful.
        /// </summary>
        Task<AuthResponseDTO> LoginAsync(string username, string password);

        /// <summary>
        /// Uses a refresh token to get a new Access Token (and possibly new Refresh Token).
        /// </summary>
        Task<AuthResponseDTO> RefreshAsync(string refreshToken);

        /// <summary>
        /// Optionally revoke all tokens for a user. 
        /// Could be used for logout everywhere or forced sign-out.
        /// </summary>
        Task RevokeAllAsync(int userId);



        Task<(bool IsValid, int UserId)> ValidateTokenAsync(string token);
    }
}

using DTOs;

namespace Services
{
    /// <summary>
    /// Defines authentication operations: login, token refresh, token validation,
    /// and revocation of all refresh tokens for a user.
    /// </summary>
    public interface IAuthenticationService
    {
        /// <summary>
        /// Validates credentials and issues new tokens.
        /// </summary>
        /// <param name="username">The user's login name.</param>
        /// <param name="password">The clear-text password.</param>
        /// <returns>
        /// An <see cref="AuthResponseDTO"/> containing access and refresh tokens,
        /// or <c>null</c> if authentication fails.
        /// </returns>
        Task<AuthResponseDTO> LoginAsync(string username, string password);

        /// <summary>
        /// Exchanges an existing refresh token for a fresh access token (and refresh token).
        /// </summary>
        /// <param name="refreshToken">The refresh token to renew.</param>
        /// <returns>
        /// A new <see cref="AuthResponseDTO"/>, or <c>null</c> if the refresh token is invalid/expired.
        /// </returns>
        Task<AuthResponseDTO> RefreshAsync(string refreshToken);

        /// <summary>
        /// Permanently revokes all refresh tokens for the specified user.
        /// </summary>
        /// <param name="userId">User whose tokens to revoke.</param>
        Task RevokeAllAsync(int userId);

        /// <summary>
        /// Validates a JWT access token, checking its signature and expiry,
        /// and returns the user ID if valid.
        /// </summary>
        /// <param name="token">The JWT string to validate.</param>
        /// <returns>
        /// A tuple: <c>IsValid</c> indicates success, and <c>UserId</c> is the "sub" claim value.
        /// </returns>
        Task<(bool IsValid, int UserId)> ValidateTokenAsync(string token);
    }
}

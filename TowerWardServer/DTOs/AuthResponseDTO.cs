namespace DTOs
{
    /// <summary>
    /// Returned to the client after successful login or token refresh.
    /// </summary>
    public class AuthResponseDTO
    {
        /// <summary>
        /// The short-lived JWT access token.
        /// </summary>
        public string AccessToken { get; set; }

        /// <summary>
        /// When the Access Token expires.
        /// </summary>
        public DateTime AccessTokenExpiry { get; set; }

        /// <summary>
        /// A longer-lived refresh token, stored in the DB.
        /// </summary>
        public string RefreshToken { get; set; }

        /// <summary>
        /// When the Refresh Token expires.
        /// </summary>
        public DateTime RefreshTokenExpiry { get; set; }
    }
}

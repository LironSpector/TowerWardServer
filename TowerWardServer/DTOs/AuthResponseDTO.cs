using System;

namespace DTOs
{
    /// <summary>
    /// Returned to the client after successful authentication,
    /// including both access and refresh tokens and their expirations.
    /// </summary>
    public class AuthResponseDTO
    {
        /// <summary>
        /// The JWT access token for authenticated requests.
        /// </summary>
        public string AccessToken { get; set; }

        /// <summary>
        /// UTC timestamp when the access token expires.
        /// </summary>
        public DateTime AccessTokenExpiry { get; set; }

        /// <summary>
        /// The long-lived refresh token stored in the database.
        /// </summary>
        public string RefreshToken { get; set; }

        /// <summary>
        /// UTC timestamp when the refresh token expires.
        /// </summary>
        public DateTime RefreshTokenExpiry { get; set; }

        /// <summary>
        /// The identifier of the user to whom these tokens belong.
        /// </summary>
        public int UserId { get; set; }
    }
}

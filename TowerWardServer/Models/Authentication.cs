using System;

namespace Models
{
    /// <summary>
    /// Represents a persisted refresh token record for a user.
    /// Used to issue new access tokens when old ones expire.
    /// </summary>
    public class Authentication
    {
        /// <summary>
        /// Primary key of the authentication record.
        /// </summary>
        public int AuthId { get; set; }

        /// <summary>
        /// Foreign key to the owning user.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// The refresh token value (e.g., a GUID).
        /// </summary>
        public string RefreshToken { get; set; }

        /// <summary>
        /// When the refresh token will expire.
        /// </summary>
        public DateTime ExpiryTime { get; set; }

        /// <summary>
        /// Timestamp when this token record was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Navigation property back to the associated User.
        /// </summary>
        public User User { get; set; }
    }
}

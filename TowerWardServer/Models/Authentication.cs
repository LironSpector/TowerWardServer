namespace Models
{
    /// <summary>
    /// Represents a refresh token record.
    /// </summary>
    public class Authentication
    {
        public int AuthId { get; set; }  // PK

        /// <summary>
        /// The user to whom this token belongs.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// A unique token (e.g. a GUID or a JWT used specifically as a refresh token).
        /// </summary>
        public string RefreshToken { get; set; }

        /// <summary>
        /// When this refresh token expires.
        /// </summary>
        public DateTime ExpiryTime { get; set; }

        /// <summary>
        /// When the token record was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        // Navigation to User entity
        public User User { get; set; }
    }
}

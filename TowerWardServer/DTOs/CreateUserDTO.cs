namespace DTOs
{
    /// <summary>
    /// Data Transfer Object for creating a new user account.
    /// </summary>
    public class CreateUserDTO
    {
        /// <summary>
        /// Desired unique username for the new account.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Plaintext password to be hashed and stored securely.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Optional avatar URL or file path for the user's profile image.
        /// </summary>
        public string Avatar { get; set; }
    }
}

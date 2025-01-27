namespace DTOs
{
    public class CreateUserDTO
    {
        /// <summary>
        /// Desired username (unique).
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Plaintext password to be hashed.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Optional avatar URL/path.
        /// </summary>
        public string Avatar { get; set; }
    }
}

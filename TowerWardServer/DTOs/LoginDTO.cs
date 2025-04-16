namespace DTOs
{
    /// <summary>
    /// Data Transfer Object used for user login requests.
    /// </summary>
    public class LoginDTO
    {
        /// <summary>
        /// The username of the user attempting to log in.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// The plaintext password provided for authentication.
        /// </summary>
        public string Password { get; set; }
    }
}

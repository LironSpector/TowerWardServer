namespace DTOs
{
    /// <summary>
    /// Data Transfer Object used to request a new access token
    /// by supplying a valid refresh token.
    /// </summary>
    public class RefreshRequestDTO
    {
        /// <summary>
        /// The refresh token previously issued to the client.
        /// </summary>
        public string RefreshToken { get; set; }
    }
}

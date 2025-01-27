namespace DTOs
{
    /// <summary>
    /// Used for refreshing an access token with an existing refresh token.
    /// </summary>
    public class RefreshRequestDTO
    {
        public string RefreshToken { get; set; }
    }
}

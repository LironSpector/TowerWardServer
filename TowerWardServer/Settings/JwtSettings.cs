namespace Settings
{
    public class JwtSettings
    {
        /// <summary>
        /// Secret key used to sign JWT tokens.
        /// </summary>
        public string SecretKey { get; set; }

        /// <summary>
        /// Issuer for the token (optional).
        /// </summary>
        public string Issuer { get; set; }

        /// <summary>
        /// Audience for the token (optional).
        /// </summary>
        public string Audience { get; set; }

        /// <summary>
        /// Access token lifespan in minutes (for short-living tokens).
        /// </summary>
        public int AccessTokenExpirationMinutes { get; set; } = 30;

        /// <summary>
        /// Refresh token lifespan in days (for longer-living tokens).
        /// </summary>
        public int RefreshTokenExpirationDays { get; set; } = 7;
    }
}

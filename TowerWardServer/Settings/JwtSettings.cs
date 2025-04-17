namespace Settings
{
    /// <summary>
    /// Configuration options for JWT token issuance and validation.
    /// Loaded from the "JwtSettings" section in appsettings.json.
    /// </summary>
    public class JwtSettings
    {
        /// <summary>
        /// Secret key used to sign and verify JWT access tokens.
        /// </summary>
        public string SecretKey { get; set; }

        /// <summary>
        /// Identifier for the token issuer (optional; may be used in validation).
        /// </summary>
        public string Issuer { get; set; }

        /// <summary>
        /// Intended audience for the token (optional; may be used in validation).
        /// </summary>
        public string Audience { get; set; }

        /// <summary>
        /// Lifespan of an access token, in minutes (short-lived token).
        /// </summary>
        public int AccessTokenExpirationMinutes { get; set; } = 30;

        /// <summary>
        /// Lifespan of a refresh token, in days (longer-lived token).
        /// </summary>
        public int RefreshTokenExpirationDays { get; set; } = 7;
    }
}

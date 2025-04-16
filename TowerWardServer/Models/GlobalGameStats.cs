namespace Models
{
    /// <summary>
    /// Represents global, system wide game statistics.
    /// Typically maintained as a single record (e.g., ID = 1).
    /// </summary>
    public class GlobalGameStats
    {
        /// <summary>
        /// Primary key of the global stats record.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Total number of registered users in the system.
        /// </summary>
        public int TotalUsers { get; set; }

        /// <summary>
        /// Total number of games played across all users.
        /// </summary>
        public long TotalGamesPlayed { get; set; }

        /// <summary>
        /// Total number of single-player games played.
        /// </summary>
        public long TotalSingleplayerGames { get; set; }

        /// <summary>
        /// Total number of multiplayer games played.
        /// </summary>
        public long TotalMultiplayerGames { get; set; }
    }
}

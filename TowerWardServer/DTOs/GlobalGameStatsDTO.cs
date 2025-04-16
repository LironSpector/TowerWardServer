namespace DTOs
{
    /// <summary>
    /// Data Transfer Object for global, system wide game statistics.
    /// </summary>
    public class GlobalGameStatsDTO
    {
        /// <summary>
        /// Primary key of the global stats record.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Total number of registered users.
        /// </summary>
        public int TotalUsers { get; set; }

        /// <summary>
        /// Total number of games played across the system.
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

using System;

namespace DTOs
{
    /// <summary>
    /// Data Transfer Object for a user's game statistics.
    /// </summary>
    public class UserGameStatsDTO
    {
        /// <summary>
        /// The user identifier to which these stats belong.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Total games played by the user.
        /// </summary>
        public int GamesPlayed { get; set; }

        /// <summary>
        /// Total games won by the user.
        /// </summary>
        public int GamesWon { get; set; }

        /// <summary>
        /// Cumulative time played (in seconds).
        /// </summary>
        public int TotalTimePlayed { get; set; }

        /// <summary>
        /// Number of single-player games played.
        /// </summary>
        public int SinglePlayerGames { get; set; }

        /// <summary>
        /// Number of multiplayer games played.
        /// </summary>
        public int MultiplayerGames { get; set; }

        /// <summary>
        /// Experience points earned by the user.
        /// </summary>
        public int Xp { get; set; }

        /// <summary>
        /// UTC timestamp of the last update to these stats.
        /// </summary>
        public DateTime LastUpdate { get; set; }
    }
}

using System;

namespace Models
{
    /// <summary>
    /// Tracks aggregated gameplay statistics for a single user.
    /// Primary key is UserId, which also serves as a foreign key to User.
    /// </summary>
    public class UserGameStats
    {
        /// <summary>
        /// Primary key and foreign key to the User entity.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Total number of games played by the user.
        /// </summary>
        public int GamesPlayed { get; set; }

        /// <summary>
        /// Total number of games the user has won.
        /// </summary>
        public int GamesWon { get; set; }

        /// <summary>
        /// Cumulative time played (in seconds).
        /// </summary>
        public int TotalTimePlayed { get; set; }

        /// <summary>
        /// Count of single-player games.
        /// </summary>
        public int SinglePlayerGames { get; set; }

        /// <summary>
        /// Count of multiplayer games.
        /// </summary>
        public int MultiplayerGames { get; set; }

        /// <summary>
        /// Experience points earned by the user.
        /// </summary>
        public int Xp { get; set; }

        /// <summary>
        /// Timestamp of the last update to these stats.
        /// </summary>
        public DateTime LastUpdate { get; set; }

        /// <summary>
        /// Navigation property back to the associated User.
        /// </summary>
        public User User { get; set; }
    }
}
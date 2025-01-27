using System;

namespace Models
{
    public class UserGameStats
    {
        public int UserId { get; set; } // PK, also FK to User
        public int GamesPlayed { get; set; }
        public int GamesWon { get; set; }
        public int TotalTimePlayed { get; set; } // in seconds or minutes
        public int SinglePlayerGames { get; set; }
        public int MultiplayerGames { get; set; }
        public int Xp { get; set; }
        public DateTime LastUpdate { get; set; }

        // Navigation property
        public User User { get; set; }
    }
}

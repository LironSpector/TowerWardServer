using System;

namespace DTOs
{
    public class UserGameStatsDTO
    {
        public int UserId { get; set; }
        public int GamesPlayed { get; set; }
        public int GamesWon { get; set; }
        public int TotalTimePlayed { get; set; }
        public int SinglePlayerGames { get; set; }
        public int MultiplayerGames { get; set; }
        public int Xp { get; set; }
        public DateTime LastUpdate { get; set; }
    }
}

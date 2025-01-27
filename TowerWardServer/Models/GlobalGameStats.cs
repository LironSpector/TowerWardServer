namespace Models
{
    public class GlobalGameStats
    {
        public int Id { get; set; }  // PK, often just 1 row in practice
        public int TotalUsers { get; set; }
        public long TotalGamesPlayed { get; set; }
        public long TotalSingleplayerGames { get; set; }
        public long TotalMultiplayerGames { get; set; }
    }
}

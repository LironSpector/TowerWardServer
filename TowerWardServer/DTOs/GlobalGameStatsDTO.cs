namespace DTOs
{
    public class GlobalGameStatsDTO
    {
        public int Id { get; set; }
        public int TotalUsers { get; set; }
        public long TotalGamesPlayed { get; set; }
        public long TotalSingleplayerGames { get; set; }
        public long TotalMultiplayerGames { get; set; }
    }
}

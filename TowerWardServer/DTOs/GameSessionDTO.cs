using System;

namespace DTOs
{
    public class GameSessionDTO
    {
        public int SessionId { get; set; }
        public int? User1Id { get; set; }
        public int? User2Id { get; set; }
        public string Mode { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int? WonUserId { get; set; }
        public int? FinalWave { get; set; }
        public int? TimePlayed { get; set; }
    }
}

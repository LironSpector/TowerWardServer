using System;

namespace Models
{
    public class GameSession
    {
        public int SessionId { get; set; } // PK
        public int? User1Id { get; set; }  // FK -> users
        public int? User2Id { get; set; }  // FK -> users
        public string Mode { get; set; }   // "SinglePlayer", "Multiplayer"
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int? WonUserId { get; set; }
        public int? FinalWave { get; set; }
        public int? TimePlayed { get; set; } // e.g. in seconds

        // Navigation properties
        public User User1 { get; set; }
        public User User2 { get; set; }
        // If the user who won is either user1 or user2, you can reference them similarly:
        // Optionally, store a reference to the winning user if you prefer:
        // public User WonUser { get; set; }
    }
}

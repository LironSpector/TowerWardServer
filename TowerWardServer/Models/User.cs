using System.Collections.Generic;
using System;

namespace Models
{
    public class User
    {
        public int UserId { get; set; }    // Maps to user_id
        public string Username { get; set; }
        public string Password { get; set; }
        public string Avatar { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
        public string Status { get; set; } // e.g. "Active", "Banned", etc.

        // Navigation property for 1-to-1 relationship with UserGameStats
        public UserGameStats UserGameStats { get; set; }

        // If you want to see all sessions in which the user is user1 or user2:
        public ICollection<GameSession> GameSessionsAsUser1 { get; set; }
        public ICollection<GameSession> GameSessionsAsUser2 { get; set; }
    }
}

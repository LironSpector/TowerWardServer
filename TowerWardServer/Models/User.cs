using System;
using System.Collections.Generic;

namespace Models
{
    /// <summary>
    /// Represents a registered user in the system.
    /// Contains authentication info, profile data, and navigation to related stats and sessions.
    /// </summary>
    public class User
    {
        /// <summary>
        /// Primary key. Maps to the database column user_id.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// The user's unique username.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// The user's password hash.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Filename or URL of the user's avatar image.
        /// </summary>
        public string Avatar { get; set; }

        /// <summary>
        /// Timestamp when the user account was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Timestamp of the user's last login, if any.
        /// </summary>
        public DateTime? LastLogin { get; set; }

        /// <summary>
        /// Current status of the user (e.g., "Active", "Banned").
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Navigation property for the user's game statistics (1-to-1 relationship).
        /// </summary>
        public UserGameStats UserGameStats { get; set; }

        /// <summary>
        /// All game sessions where this user was the first participant.
        /// </summary>
        public ICollection<GameSession> GameSessionsAsUser1 { get; set; }

        /// <summary>
        /// All game sessions where this user was the second participant.
        /// </summary>
        public ICollection<GameSession> GameSessionsAsUser2 { get; set; }
    }
}

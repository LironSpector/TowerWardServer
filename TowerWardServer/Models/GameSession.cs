using System;

namespace Models
{
    /// <summary>
    /// Represents a single play session, either single-player or multiplayer.
    /// Stores participants, outcome, and timing information.
    /// </summary>
    public class GameSession
    {
        /// <summary>
        /// Primary key of the session.
        /// </summary>
        public int SessionId { get; set; }

        /// <summary>
        /// Foreign key to the first user (participant 1), if any.
        /// </summary>
        public int? User1Id { get; set; }

        /// <summary>
        /// Foreign key to the second user (participant 2), if any.
        /// </summary>
        public int? User2Id { get; set; }

        /// <summary>
        /// Mode of play: "SinglePlayer" or "Multiplayer".
        /// </summary>
        public string Mode { get; set; }

        /// <summary>
        /// Timestamp when the session started.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Timestamp when the session ended, if ended.
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// UserId of the winner, if any.
        /// </summary>
        public int? WonUserId { get; set; }

        /// <summary>
        /// The final wave reached in the session, if tracked.
        /// </summary>
        public int? FinalWave { get; set; }

        /// <summary>
        /// Total time played in this session, in seconds.
        /// </summary>
        public int? TimePlayed { get; set; }

        /// <summary>
        /// Navigation property to participant 1.
        /// </summary>
        public User User1 { get; set; }

        /// <summary>
        /// Navigation property to participant 2.
        /// </summary>
        public User User2 { get; set; }
    }
}

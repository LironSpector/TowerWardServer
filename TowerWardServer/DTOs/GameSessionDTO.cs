using System;

namespace DTOs
{
    /// <summary>
    /// Data Transfer Object representing a game session's data.
    /// </summary>
    public class GameSessionDTO
    {
        /// <summary>
        /// Unique identifier of the game session.
        /// </summary>
        public int SessionId { get; set; }

        /// <summary>
        /// UserId of the first participant, if any.
        /// </summary>
        public int? User1Id { get; set; }

        /// <summary>
        /// UserId of the second participant, if any.
        /// </summary>
        public int? User2Id { get; set; }

        /// <summary>
        /// Mode of the game ("SinglePlayer" or "Multiplayer").
        /// </summary>
        public string Mode { get; set; }

        /// <summary>
        /// UTC timestamp when the session started.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// UTC timestamp when the session ended, if ended.
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// UserId of the winning participant, if any.
        /// </summary>
        public int? WonUserId { get; set; }

        /// <summary>
        /// The final wave reached in the session.
        /// </summary>
        public int? FinalWave { get; set; }

        /// <summary>
        /// Total time played during the session (in seconds).
        /// </summary>
        public int? TimePlayed { get; set; }
    }
}

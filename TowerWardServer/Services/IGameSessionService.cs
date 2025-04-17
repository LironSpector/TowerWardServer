using DTOs;

namespace Services
{
    /// <summary>
    /// Defines business logic operations for managing game sessions.
    /// </summary>
    public interface IGameSessionService
    {
        /// <summary>
        /// Retrieves a game session by its unique identifier.
        /// </summary>
        /// <param name="sessionId">The unique identifier of the session.</param>
        /// <returns>
        /// A <see cref="GameSessionDTO"/> representing the session,
        /// or <c>null</c> if not found.
        /// </returns>
        Task<GameSessionDTO> GetSessionByIdAsync(int sessionId);

        /// <summary>
        /// Retrieves all game sessions.
        /// </summary>
        /// <returns>
        /// A collection of <see cref="GameSessionDTO"/> for all sessions.
        /// </returns>
        Task<IEnumerable<GameSessionDTO>> GetAllSessionsAsync();

        /// <summary>
        /// Creates a new game session record.
        /// </summary>
        /// <param name="sessionDto">
        /// A <see cref="GameSessionDTO"/> containing initial session data.
        /// </param>
        /// <returns>
        /// The generated session ID.
        /// </returns>
        Task<int> CreateSessionAsync(GameSessionDTO sessionDto);

        /// <summary>
        /// Updates an existing game session.
        /// </summary>
        /// <param name="sessionDto">
        /// A <see cref="GameSessionDTO"/> containing updated session data.
        /// </param>
        /// <exception cref="System.Exception">
        /// Thrown if the session is not found.
        /// </exception>
        Task UpdateSessionAsync(GameSessionDTO sessionDto);

        /// <summary>
        /// Deletes a game session by its ID.
        /// </summary>
        /// <param name="sessionId">The unique identifier of the session.</param>
        /// <exception cref="System.Exception">
        /// Thrown if the session is not found.
        /// </exception>
        Task DeleteSessionAsync(int sessionId);

        /// <summary>
        /// Marks a session as ended, setting end time and final stats.
        /// </summary>
        /// <param name="sessionId">The unique identifier of the session.</param>
        /// <param name="wonUserId">
        /// The ID of the winning user, or <c>null</c> for no winner.
        /// </param>
        /// <param name="finalWave">The final wave reached, or <c>null</c>.</param>
        /// <param name="timePlayed">
        /// The total time played in seconds, or <c>null</c>.
        /// </param>
        /// <exception cref="System.Exception">
        /// Thrown if the session is not found.
        /// </exception>
        Task EndSessionAsync(int sessionId, int? wonUserId, int? finalWave, int? timePlayed);
    }
}

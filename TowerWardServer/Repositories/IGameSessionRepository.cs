using Models;

namespace Repositories
{
    /// <summary>
    /// Defines data access methods for <see cref="GameSession"/> entities.
    /// </summary>
    public interface IGameSessionRepository
    {
        /// <summary>
        /// Retrieves a <see cref="GameSession"/> by its primary key.
        /// </summary>
        Task<GameSession> GetByIdAsync(int sessionId);

        /// <summary>
        /// Retrieves all <see cref="GameSession"/> records.
        /// </summary>
        Task<IEnumerable<GameSession>> GetAllAsync();

        /// <summary>
        /// Adds a new <see cref="GameSession"/> record.
        /// </summary>
        Task AddAsync(GameSession session);

        /// <summary>
        /// Updates an existing <see cref="GameSession"/> record.
        /// </summary>
        Task UpdateAsync(GameSession session);

        /// <summary>
        /// Deletes a <see cref="GameSession"/> record.
        /// </summary>
        Task DeleteAsync(GameSession session);
    }
}

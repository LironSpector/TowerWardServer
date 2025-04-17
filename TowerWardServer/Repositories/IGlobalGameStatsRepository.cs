using Models;

namespace Repositories
{
    /// <summary>
    /// Defines data access methods for <see cref="GlobalGameStats"/> entities.
    /// </summary>
    public interface IGlobalGameStatsRepository
    {
        /// <summary>
        /// Retrieves a <see cref="GlobalGameStats"/> record by its primary key.
        /// </summary>
        Task<GlobalGameStats> GetByIdAsync(int id);

        /// <summary>
        /// Adds a new <see cref="GlobalGameStats"/> record.
        /// </summary>
        Task AddAsync(GlobalGameStats stats);

        /// <summary>
        /// Updates an existing <see cref="GlobalGameStats"/> record.
        /// </summary>
        Task UpdateAsync(GlobalGameStats stats);

        /// <summary>
        /// Deletes a <see cref="GlobalGameStats"/> record.
        /// </summary>
        Task DeleteAsync(GlobalGameStats stats);
    }
}

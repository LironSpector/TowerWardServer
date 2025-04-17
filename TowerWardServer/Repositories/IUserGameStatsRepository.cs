using Models;

namespace Repositories
{
    /// <summary>
    /// Defines data access methods for <see cref="UserGameStats"/> entities.
    /// </summary>
    public interface IUserGameStatsRepository
    {
        /// <summary>
        /// Retrieves the game statistics record for a given user.
        /// </summary>
        Task<UserGameStats> GetByUserIdAsync(int userId);

        /// <summary>
        /// Adds a new <see cref="UserGameStats"/> record.
        /// </summary>
        Task AddAsync(UserGameStats stats);

        /// <summary>
        /// Updates an existing <see cref="UserGameStats"/> record.
        /// </summary>
        Task UpdateAsync(UserGameStats stats);

        /// <summary>
        /// Deletes a <see cref="UserGameStats"/> record.
        /// </summary>
        Task DeleteAsync(UserGameStats stats);
    }
}

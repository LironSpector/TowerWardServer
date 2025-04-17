using Models;

namespace Repositories
{
    /// <summary>
    /// Defines data access methods for refresh-token <see cref="Authentication"/> records.
    /// </summary>
    public interface IAuthenticationRepository
    {
        /// <summary>
        /// Retrieves an <see cref="Authentication"/> record by its primary key.
        /// </summary>
        Task<Authentication> GetByIdAsync(int authId);

        /// <summary>
        /// Retrieves a single <see cref="Authentication"/> record matching the given refresh token.
        /// </summary>
        Task<Authentication> GetByRefreshTokenAsync(string refreshToken);

        /// <summary>
        /// Retrieves all <see cref="Authentication"/> records for the specified user.
        /// </summary>
        Task<IEnumerable<Authentication>> GetAllForUserAsync(int userId);

        /// <summary>
        /// Adds a new <see cref="Authentication"/> record to the database.
        /// </summary>
        Task AddAsync(Authentication authRecord);

        /// <summary>
        /// Updates an existing <see cref="Authentication"/> record.
        /// </summary>
        Task UpdateAsync(Authentication authRecord);

        /// <summary>
        /// Deletes a specific <see cref="Authentication"/> record.
        /// </summary>
        Task DeleteAsync(Authentication authRecord);

        /// <summary>
        /// Revokes (deletes) all refresh-token records for a given user.
        /// </summary>
        Task RevokeAllTokensForUserAsync(int userId);
    }
}

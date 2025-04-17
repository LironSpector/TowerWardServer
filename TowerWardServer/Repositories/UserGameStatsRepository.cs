using Microsoft.EntityFrameworkCore;
using Models;
using Database;

namespace Repositories
{
    /// <summary>
    /// Implements <see cref="IUserGameStatsRepository"/> using Entity Framework Core.
    /// Provides CRUD operations for <see cref="UserGameStats"/> entities.
    /// </summary>
    public class UserGameStatsRepository : IUserGameStatsRepository
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Constructs a new repository with the given EF Core context.
        /// </summary>
        public UserGameStatsRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc/>
        public async Task<UserGameStats> GetByUserIdAsync(int userId)
        {
            return await _context.UserGameStats
                .FirstOrDefaultAsync(ugs => ugs.UserId == userId);
        }

        /// <inheritdoc/>
        public async Task AddAsync(UserGameStats stats)
        {
            await _context.UserGameStats.AddAsync(stats);
            await _context.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public async Task UpdateAsync(UserGameStats stats)
        {
            _context.UserGameStats.Update(stats);
            await _context.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public async Task DeleteAsync(UserGameStats stats)
        {
            _context.UserGameStats.Remove(stats);
            await _context.SaveChangesAsync();
        }
    }
}

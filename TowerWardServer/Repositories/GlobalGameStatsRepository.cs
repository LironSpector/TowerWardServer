using Microsoft.EntityFrameworkCore;
using Models;
using Database;

namespace Repositories
{
    /// <summary>
    /// Implements <see cref="IGlobalGameStatsRepository"/> using Entity Framework Core.
    /// Performs CRUD operations on <see cref="GlobalGameStats"/> entities.
    /// </summary>
    public class GlobalGameStatsRepository : IGlobalGameStatsRepository
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Constructs a new repository with the provided EF Core context.
        /// </summary>
        public GlobalGameStatsRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc/>
        public async Task<GlobalGameStats> GetByIdAsync(int id)
        {
            return await _context.GlobalGameStats
                .FirstOrDefaultAsync(g => g.Id == id);
        }

        /// <inheritdoc/>
        public async Task AddAsync(GlobalGameStats stats)
        {
            await _context.GlobalGameStats.AddAsync(stats);
            await _context.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public async Task UpdateAsync(GlobalGameStats stats)
        {
            _context.GlobalGameStats.Update(stats);
            await _context.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public async Task DeleteAsync(GlobalGameStats stats)
        {
            _context.GlobalGameStats.Remove(stats);
            await _context.SaveChangesAsync();
        }
    }
}

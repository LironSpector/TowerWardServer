using Microsoft.EntityFrameworkCore;
using Models;
using Database;
using System.Threading.Tasks;

namespace Repositories
{
    public class GlobalGameStatsRepository : IGlobalGameStatsRepository
    {
        private readonly ApplicationDbContext _context;

        public GlobalGameStatsRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<GlobalGameStats> GetByIdAsync(int id)
        {
            return await _context.GlobalGameStats
                .FirstOrDefaultAsync(g => g.Id == id);
        }

        public async Task AddAsync(GlobalGameStats stats)
        {
            await _context.GlobalGameStats.AddAsync(stats);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(GlobalGameStats stats)
        {
            _context.GlobalGameStats.Update(stats);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(GlobalGameStats stats)
        {
            _context.GlobalGameStats.Remove(stats);
            await _context.SaveChangesAsync();
        }
    }
}

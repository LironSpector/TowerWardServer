using Microsoft.EntityFrameworkCore;
using Models;
using Database;
using System.Threading.Tasks;

namespace Repositories
{
    public class UserGameStatsRepository : IUserGameStatsRepository
    {
        private readonly ApplicationDbContext _context;

        public UserGameStatsRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<UserGameStats> GetByUserIdAsync(int userId)
        {
            return await _context.UserGameStats
                .FirstOrDefaultAsync(ugs => ugs.UserId == userId);
        }

        public async Task AddAsync(UserGameStats stats)
        {
            await _context.UserGameStats.AddAsync(stats);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(UserGameStats stats)
        {
            _context.UserGameStats.Update(stats);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(UserGameStats stats)
        {
            _context.UserGameStats.Remove(stats);
            await _context.SaveChangesAsync();
        }
    }
}

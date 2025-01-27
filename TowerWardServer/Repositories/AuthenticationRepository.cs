using Microsoft.EntityFrameworkCore;
using Models;
using Database;

namespace Repositories
{
    public class AuthenticationRepository : IAuthenticationRepository
    {
        private readonly ApplicationDbContext _context;

        public AuthenticationRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Authentication> GetByIdAsync(int authId)
        {
            return await _context.Set<Authentication>()
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.AuthId == authId);
        }

        public async Task<Authentication> GetByRefreshTokenAsync(string refreshToken)
        {
            return await _context.Set<Authentication>()
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.RefreshToken == refreshToken);
        }

        public async Task<IEnumerable<Authentication>> GetAllForUserAsync(int userId)
        {
            return await _context.Set<Authentication>()
                .Where(a => a.UserId == userId)
                .Include(a => a.User)
                .ToListAsync();
        }

        public async Task AddAsync(Authentication authRecord)
        {
            await _context.Set<Authentication>().AddAsync(authRecord);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Authentication authRecord)
        {
            _context.Set<Authentication>().Update(authRecord);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Authentication authRecord)
        {
            _context.Set<Authentication>().Remove(authRecord);
            await _context.SaveChangesAsync();
        }

        public async Task RevokeAllTokensForUserAsync(int userId)
        {
            var records = _context.Set<Authentication>().Where(a => a.UserId == userId);
            _context.Set<Authentication>().RemoveRange(records);
            await _context.SaveChangesAsync();
        }
    }
}

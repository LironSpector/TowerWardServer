using Microsoft.EntityFrameworkCore;
using Models;
using Database;

namespace Repositories
{
    /// <summary>
    /// Implements <see cref="IAuthenticationRepository"/> using Entity Framework Core.
    /// Manages CRUD and revocation operations for refresh-token records.
    /// </summary>
    public class AuthenticationRepository : IAuthenticationRepository
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Initializes a new instance with the given EF Core context.
        /// </summary>
        public AuthenticationRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc/>
        public async Task<Authentication> GetByIdAsync(int authId)
        {
            return await _context.Set<Authentication>()
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.AuthId == authId);
        }

        /// <inheritdoc/>
        public async Task<Authentication> GetByRefreshTokenAsync(string refreshToken)
        {
            Console.WriteLine("Refresh token: " + refreshToken);
            return await _context.Set<Authentication>()
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.RefreshToken == refreshToken);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Authentication>> GetAllForUserAsync(int userId)
        {
            return await _context.Set<Authentication>()
                .Where(a => a.UserId == userId)
                .Include(a => a.User)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task AddAsync(Authentication authRecord)
        {
            Console.WriteLine("Adding auth record");
            await _context.Set<Authentication>().AddAsync(authRecord);
            await _context.SaveChangesAsync();
            Console.WriteLine("Auth record added");
        }

        /// <inheritdoc/>
        public async Task UpdateAsync(Authentication authRecord)
        {
            _context.Set<Authentication>().Update(authRecord);
            await _context.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public async Task DeleteAsync(Authentication authRecord)
        {
            _context.Set<Authentication>().Remove(authRecord);
            await _context.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public async Task RevokeAllTokensForUserAsync(int userId)
        {
            var records = _context.Set<Authentication>().Where(a => a.UserId == userId);
            _context.Set<Authentication>().RemoveRange(records);
            await _context.SaveChangesAsync();
        }
    }
}

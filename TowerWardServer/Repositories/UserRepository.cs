using Microsoft.EntityFrameworkCore;
using Models;
using Database;

namespace Repositories
{
    /// <summary>
    /// Implements <see cref="IUserRepository"/> using Entity Framework Core.
    /// Provides CRUD operations for <see cref="User"/> entities.
    /// </summary>
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Constructs a new instance with the given EF Core context.
        /// </summary>
        public UserRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc/>
        public async Task<User> GetByIdAsync(int userId)
        {
            return await _context.Users
                .Include(u => u.UserGameStats)
                .FirstOrDefaultAsync(u => u.UserId == userId);
        }

        /// <inheritdoc/>
        public async Task<User> GetByUsernameAsync(string username)
        {
            return await _context.Users
                .Include(u => u.UserGameStats)
                .FirstOrDefaultAsync(u => u.Username == username);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _context.Users
                .Include(u => u.UserGameStats)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task AddAsync(User user)
        {
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public async Task UpdateAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public async Task DeleteAsync(User user)
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }
    }
}

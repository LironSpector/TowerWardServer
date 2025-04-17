using Microsoft.EntityFrameworkCore;
using Models;
using Database;

namespace Repositories
{
    /// <summary>
    /// Implements <see cref="IGameSessionRepository"/> using Entity Framework Core.
    /// Handles CRUD operations for <see cref="GameSession"/> entities.
    /// </summary>
    public class GameSessionRepository : IGameSessionRepository
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Initializes a new instance with the given EF Core context.
        /// </summary>
        public GameSessionRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc/>
        public async Task<GameSession> GetByIdAsync(int sessionId)
        {
            return await _context.GameSessions
                .Include(gs => gs.User1)
                .Include(gs => gs.User2)
                .FirstOrDefaultAsync(gs => gs.SessionId == sessionId);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<GameSession>> GetAllAsync()
        {
            return await _context.GameSessions
                .Include(gs => gs.User1)
                .Include(gs => gs.User2)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task AddAsync(GameSession session)
        {
            await _context.GameSessions.AddAsync(session);
            await _context.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public async Task UpdateAsync(GameSession session)
        {
            _context.GameSessions.Update(session);
            await _context.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public async Task DeleteAsync(GameSession session)
        {
            _context.GameSessions.Remove(session);
            await _context.SaveChangesAsync();
        }
    }
}

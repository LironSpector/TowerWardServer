using Microsoft.EntityFrameworkCore;
using Models;
using Database;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repositories
{
    public class GameSessionRepository : IGameSessionRepository
    {
        private readonly ApplicationDbContext _context;

        public GameSessionRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<GameSession> GetByIdAsync(int sessionId)
        {
            return await _context.GameSessions
                .Include(gs => gs.User1)
                .Include(gs => gs.User2)
                .FirstOrDefaultAsync(gs => gs.SessionId == sessionId);
        }

        public async Task<IEnumerable<GameSession>> GetAllAsync()
        {
            return await _context.GameSessions
                .Include(gs => gs.User1)
                .Include(gs => gs.User2)
                .ToListAsync();
        }

        public async Task AddAsync(GameSession session)
        {
            await _context.GameSessions.AddAsync(session);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(GameSession session)
        {
            _context.GameSessions.Update(session);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(GameSession session)
        {
            _context.GameSessions.Remove(session);
            await _context.SaveChangesAsync();
        }
    }
}

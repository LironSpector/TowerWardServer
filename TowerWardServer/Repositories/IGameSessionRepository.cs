using System.Collections.Generic;
using System.Threading.Tasks;
using Models;

namespace Repositories
{
    public interface IGameSessionRepository
    {
        Task<GameSession> GetByIdAsync(int sessionId);
        Task<IEnumerable<GameSession>> GetAllAsync();
        Task AddAsync(GameSession session);
        Task UpdateAsync(GameSession session);
        Task DeleteAsync(GameSession session);
    }
}

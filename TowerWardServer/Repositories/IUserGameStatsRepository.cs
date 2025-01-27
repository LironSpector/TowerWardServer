using System.Threading.Tasks;
using Models;

namespace Repositories
{
    public interface IUserGameStatsRepository
    {
        Task<UserGameStats> GetByUserIdAsync(int userId);
        Task AddAsync(UserGameStats stats);
        Task UpdateAsync(UserGameStats stats);
        Task DeleteAsync(UserGameStats stats);
    }
}

using System.Threading.Tasks;
using Models;

namespace Repositories
{
    public interface IGlobalGameStatsRepository
    {
        Task<GlobalGameStats> GetByIdAsync(int id);
        Task AddAsync(GlobalGameStats stats);
        Task UpdateAsync(GlobalGameStats stats);
        Task DeleteAsync(GlobalGameStats stats);
    }
}

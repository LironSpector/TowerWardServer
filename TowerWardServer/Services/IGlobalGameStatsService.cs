using DTOs;

namespace Services
{
    public interface IGlobalGameStatsService
    {
        Task<GlobalGameStatsDTO> GetGlobalStatsAsync(int id);  // usually id=1
        Task CreateGlobalStatsAsync(GlobalGameStatsDTO statsDto);
        Task UpdateGlobalStatsAsync(GlobalGameStatsDTO statsDto);
        Task DeleteGlobalStatsAsync(int id);

        // Additional convenience methods:
        Task IncrementTotalUsersAsync(int id, int amount = 1);
        Task IncrementGamesPlayedAsync(int id, bool singlePlayer);
    }
}

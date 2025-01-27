using DTOs;

namespace Services
{
    public interface IUserGameStatsService
    {
        Task<UserGameStatsDTO> GetStatsByUserIdAsync(int userId);
        Task CreateStatsForUserAsync(int userId); // e.g., on user creation
        Task UpdateStatsAsync(UserGameStatsDTO statsDto);
        Task DeleteStatsAsync(int userId);

        // Additional convenience methods:
        Task AddXpAsync(int userId, int amount);
        Task IncrementGamesPlayedAsync(int userId, bool won, bool singlePlayer);
    }
}

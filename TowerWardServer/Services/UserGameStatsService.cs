using DTOs;
using Models;
using Repositories;

namespace Services
{
    public class UserGameStatsService : IUserGameStatsService
    {
        private readonly IUserGameStatsRepository _statsRepository;

        public UserGameStatsService(IUserGameStatsRepository statsRepository)
        {
            _statsRepository = statsRepository;
        }

        public async Task<UserGameStatsDTO> GetStatsByUserIdAsync(int userId)
        {
            var stats = await _statsRepository.GetByUserIdAsync(userId);
            if (stats == null) return null;

            return MapToDTO(stats);
        }

        public async Task CreateStatsForUserAsync(int userId)
        {
            // Typically, you'd first check if stats already exist
            var existing = await _statsRepository.GetByUserIdAsync(userId);
            if (existing != null)
                throw new Exception($"Stats already exist for user {userId}.");

            var newStats = new UserGameStats
            {
                UserId = userId,
                GamesPlayed = 0,
                GamesWon = 0,
                TotalTimePlayed = 0,
                SinglePlayerGames = 0,
                MultiplayerGames = 0,
                Xp = 0,
                LastUpdate = DateTime.UtcNow
            };

            await _statsRepository.AddAsync(newStats);
        }

        public async Task UpdateStatsAsync(UserGameStatsDTO statsDto)
        {
            var stats = await _statsRepository.GetByUserIdAsync(statsDto.UserId);
            if (stats == null)
                throw new Exception($"Stats not found for user {statsDto.UserId}.");

            stats.GamesPlayed = statsDto.GamesPlayed;
            stats.GamesWon = statsDto.GamesWon;
            stats.TotalTimePlayed = statsDto.TotalTimePlayed;
            stats.SinglePlayerGames = statsDto.SinglePlayerGames;
            stats.MultiplayerGames = statsDto.MultiplayerGames;
            stats.Xp = statsDto.Xp;
            stats.LastUpdate = DateTime.UtcNow;

            await _statsRepository.UpdateAsync(stats);
        }

        public async Task DeleteStatsAsync(int userId)
        {
            var stats = await _statsRepository.GetByUserIdAsync(userId);
            if (stats == null)
                throw new Exception($"Stats not found for user {userId}.");

            await _statsRepository.DeleteAsync(stats);
        }

        public async Task AddXpAsync(int userId, int amount)
        {
            var stats = await _statsRepository.GetByUserIdAsync(userId);
            if (stats == null)
                throw new Exception($"Stats not found for user {userId}.");

            stats.Xp += amount;
            stats.LastUpdate = DateTime.UtcNow;
            await _statsRepository.UpdateAsync(stats);
        }

        public async Task IncrementGamesPlayedAsync(int userId, bool won, bool singlePlayer)
        {
            var stats = await _statsRepository.GetByUserIdAsync(userId);
            if (stats == null)
                throw new Exception($"Stats not found for user {userId}.");

            stats.GamesPlayed += 1;
            if (won) stats.GamesWon += 1;

            if (singlePlayer) stats.SinglePlayerGames += 1;
            else stats.MultiplayerGames += 1;

            stats.LastUpdate = DateTime.UtcNow;
            await _statsRepository.UpdateAsync(stats);
        }

        // -----------------------------------------------------------------
        // PRIVATE HELPER
        // -----------------------------------------------------------------

        private UserGameStatsDTO MapToDTO(UserGameStats entity)
        {
            return new UserGameStatsDTO
            {
                UserId = entity.UserId,
                GamesPlayed = entity.GamesPlayed,
                GamesWon = entity.GamesWon,
                TotalTimePlayed = entity.TotalTimePlayed,
                SinglePlayerGames = entity.SinglePlayerGames,
                MultiplayerGames = entity.MultiplayerGames,
                Xp = entity.Xp,
                LastUpdate = entity.LastUpdate
            };
        }
    }
}

using DTOs;
using Models;
using Repositories;

namespace Services
{
    /// <summary>
    /// Implements <see cref="IUserGameStatsService"/> to manage creation,
    /// retrieval, updating, and deletion of user game statistics.
    /// </summary>
    public class UserGameStatsService : IUserGameStatsService
    {
        private readonly IUserGameStatsRepository _statsRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserGameStatsService"/> class.
        /// </summary>
        /// <param name="statsRepository">Repository for persisting game stats.</param>
        public UserGameStatsService(IUserGameStatsRepository statsRepository)
        {
            _statsRepository = statsRepository;
        }

        /// <inheritdoc/>
        public async Task<UserGameStatsDTO> GetStatsByUserIdAsync(int userId)
        {
            var stats = await _statsRepository.GetByUserIdAsync(userId);
            if (stats == null) return null;
            return MapToDTO(stats);
        }

        /// <inheritdoc/>
        public async Task CreateStatsForUserAsync(int userId)
        {
            var existing = await _statsRepository.GetByUserIdAsync(userId);
            if (existing != null)
            {
                throw new Exception($"Stats already exist for user {userId}.");
            }

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

        /// <inheritdoc/>
        public async Task UpdateStatsAsync(UserGameStatsDTO statsDto)
        {
            var stats = await _statsRepository.GetByUserIdAsync(statsDto.UserId);
            if (stats == null)
            {
                throw new Exception($"Stats not found for user {statsDto.UserId}.");
            }

            stats.GamesPlayed = statsDto.GamesPlayed;
            stats.GamesWon = statsDto.GamesWon;
            stats.TotalTimePlayed = statsDto.TotalTimePlayed;
            stats.SinglePlayerGames = statsDto.SinglePlayerGames;
            stats.MultiplayerGames = statsDto.MultiplayerGames;
            stats.Xp = statsDto.Xp;
            stats.LastUpdate = DateTime.UtcNow;

            await _statsRepository.UpdateAsync(stats);
        }

        /// <inheritdoc/>
        public async Task DeleteStatsAsync(int userId)
        {
            var stats = await _statsRepository.GetByUserIdAsync(userId);
            if (stats == null)
            {
                throw new Exception($"Stats not found for user {userId}.");
            }

            await _statsRepository.DeleteAsync(stats);
        }

        /// <inheritdoc/>
        public async Task AddXpAsync(int userId, int amount)
        {
            var stats = await _statsRepository.GetByUserIdAsync(userId);
            if (stats == null)
            {
                throw new Exception($"Stats not found for user {userId}.");
            }

            stats.Xp += amount;
            stats.LastUpdate = DateTime.UtcNow;
            await _statsRepository.UpdateAsync(stats);
        }

        /// <inheritdoc/>
        public async Task IncrementGamesPlayedAsync(int userId, bool won, bool singlePlayer)
        {
            var stats = await _statsRepository.GetByUserIdAsync(userId);
            if (stats == null)
            {
                throw new Exception($"Stats not found for user {userId}.");
            }

            stats.GamesPlayed++;
            if (won) stats.GamesWon++;
            if (singlePlayer) stats.SinglePlayerGames++;
            else stats.MultiplayerGames++;

            stats.LastUpdate = DateTime.UtcNow;
            await _statsRepository.UpdateAsync(stats);
        }

        // -----------------------------------------------------------------
        // PRIVATE HELPER
        // -----------------------------------------------------------------

        /// <summary>
        /// Maps a <see cref="UserGameStats"/> entity to a <see cref="UserGameStatsDTO"/>.
        /// </summary>
        /// <param name="entity">The source stats entity.</param>
        /// <returns>The resulting stats DTO.</returns>
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

using DTOs;
using Models;
using Repositories;

namespace Services
{
    /// <summary>
    /// Implements <see cref="IGlobalGameStatsService"/> to manage
    /// creation, retrieval, updating, and deletion of global game statistics,
    /// as well as convenience increment methods.
    /// </summary>
    public class GlobalGameStatsService : IGlobalGameStatsService
    {
        private readonly IGlobalGameStatsRepository _globalStatsRepo;

        /// <summary>
        /// Initializes a new instance of <see cref="GlobalGameStatsService"/>.
        /// </summary>
        /// <param name="globalStatsRepo">
        /// Repository for accessing <see cref="GlobalGameStats"/> entities.
        /// </param>
        public GlobalGameStatsService(IGlobalGameStatsRepository globalStatsRepo)
        {
            _globalStatsRepo = globalStatsRepo;
        }

        /// <inheritdoc/>
        public async Task<GlobalGameStatsDTO> GetGlobalStatsAsync(int id)
        {
            var stats = await _globalStatsRepo.GetByIdAsync(id);
            if (stats == null) return null;
            return MapToDTO(stats);
        }

        /// <inheritdoc/>
        public async Task CreateGlobalStatsAsync(GlobalGameStatsDTO statsDto)
        {
            var existing = await _globalStatsRepo.GetByIdAsync(statsDto.Id);
            if (existing != null)
                throw new Exception($"Global stats with id={statsDto.Id} already exist.");

            var entity = new GlobalGameStats
            {
                Id = statsDto.Id,
                TotalUsers = statsDto.TotalUsers,
                TotalGamesPlayed = statsDto.TotalGamesPlayed,
                TotalSingleplayerGames = statsDto.TotalSingleplayerGames,
                TotalMultiplayerGames = statsDto.TotalMultiplayerGames
            };

            await _globalStatsRepo.AddAsync(entity);
        }

        /// <inheritdoc/>
        public async Task UpdateGlobalStatsAsync(GlobalGameStatsDTO statsDto)
        {
            var entity = await _globalStatsRepo.GetByIdAsync(statsDto.Id);
            if (entity == null)
                throw new Exception($"Global stats with id={statsDto.Id} not found.");

            entity.TotalUsers = statsDto.TotalUsers;
            entity.TotalGamesPlayed = statsDto.TotalGamesPlayed;
            entity.TotalSingleplayerGames = statsDto.TotalSingleplayerGames;
            entity.TotalMultiplayerGames = statsDto.TotalMultiplayerGames;

            await _globalStatsRepo.UpdateAsync(entity);
        }

        /// <inheritdoc/>
        public async Task DeleteGlobalStatsAsync(int id)
        {
            var entity = await _globalStatsRepo.GetByIdAsync(id);
            if (entity == null)
                throw new Exception($"Global stats with id={id} not found.");

            await _globalStatsRepo.DeleteAsync(entity);
        }

        /// <inheritdoc/>
        public async Task IncrementTotalUsersAsync(int id, int amount = 1)
        {
            var stats = await _globalStatsRepo.GetByIdAsync(id);
            if (stats == null)
                throw new Exception($"Global stats with id={id} not found.");

            stats.TotalUsers += amount;
            await _globalStatsRepo.UpdateAsync(stats);
        }

        /// <inheritdoc/>
        public async Task IncrementGamesPlayedAsync(int id, bool singlePlayer)
        {
            var stats = await _globalStatsRepo.GetByIdAsync(id);
            if (stats == null)
                throw new Exception($"Global stats with id={id} not found.");

            stats.TotalGamesPlayed += 1;
            if (singlePlayer)
                stats.TotalSingleplayerGames += 1;
            else
                stats.TotalMultiplayerGames += 1;

            await _globalStatsRepo.UpdateAsync(stats);
        }

        /// <summary>
        /// Maps a <see cref="GlobalGameStats"/> entity to its corresponding DTO.
        /// </summary>
        /// <param name="entity">The entity to map.</param>
        /// <returns>A <see cref="GlobalGameStatsDTO"/> with the mapped values.</returns>
        private GlobalGameStatsDTO MapToDTO(GlobalGameStats entity)
        {
            return new GlobalGameStatsDTO
            {
                Id = entity.Id,
                TotalUsers = entity.TotalUsers,
                TotalGamesPlayed = entity.TotalGamesPlayed,
                TotalSingleplayerGames = entity.TotalSingleplayerGames,
                TotalMultiplayerGames = entity.TotalMultiplayerGames
            };
        }
    }
}

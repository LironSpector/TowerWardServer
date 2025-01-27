using DTOs;
using Models;
using Repositories;

namespace Services
{
    public class GlobalGameStatsService : IGlobalGameStatsService
    {
        private readonly IGlobalGameStatsRepository _globalStatsRepo;

        public GlobalGameStatsService(IGlobalGameStatsRepository globalStatsRepo)
        {
            _globalStatsRepo = globalStatsRepo;
        }

        public async Task<GlobalGameStatsDTO> GetGlobalStatsAsync(int id)
        {
            var stats = await _globalStatsRepo.GetByIdAsync(id);
            if (stats == null) return null;

            return MapToDTO(stats);
        }

        public async Task CreateGlobalStatsAsync(GlobalGameStatsDTO statsDto)
        {
            // Usually you'd check if a record for the same ID already exists
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

        public async Task DeleteGlobalStatsAsync(int id)
        {
            var entity = await _globalStatsRepo.GetByIdAsync(id);
            if (entity == null)
                throw new Exception($"Global stats with id={id} not found.");

            await _globalStatsRepo.DeleteAsync(entity);
        }

        public async Task IncrementTotalUsersAsync(int id, int amount = 1)
        {
            var stats = await _globalStatsRepo.GetByIdAsync(id);
            if (stats == null)
                throw new Exception($"Global stats with id={id} not found.");

            stats.TotalUsers += amount;
            await _globalStatsRepo.UpdateAsync(stats);
        }

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

        // -----------------------------------------------------------------
        // PRIVATE HELPER
        // -----------------------------------------------------------------

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

using DTOs;
using Models;
using Repositories;

namespace Services
{
    /// <summary>
    /// Implements <see cref="IGameSessionService"/> to manage creation,
    /// retrieval, updating, and deletion of game sessions.
    /// </summary>
    public class GameSessionService : IGameSessionService
    {
        private readonly IGameSessionRepository _gameSessionRepository;

        /// <summary>
        /// Initializes a new instance of <see cref="GameSessionService"/>.
        /// </summary>
        /// <param name="gameSessionRepository">
        /// Repository for persisting game session data.
        /// </param>
        public GameSessionService(IGameSessionRepository gameSessionRepository)
        {
            _gameSessionRepository = gameSessionRepository;
        }

        /// <inheritdoc/>
        public async Task<GameSessionDTO> GetSessionByIdAsync(int sessionId)
        {
            var session = await _gameSessionRepository.GetByIdAsync(sessionId);
            if (session == null) return null;
            return MapToDTO(session);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<GameSessionDTO>> GetAllSessionsAsync()
        {
            var sessions = await _gameSessionRepository.GetAllAsync();
            return sessions.Select(s => MapToDTO(s));
        }

        /// <inheritdoc/>
        public async Task<int> CreateSessionAsync(GameSessionDTO sessionDto)
        {
            var session = new GameSession
            {
                User1Id = sessionDto.User1Id,
                User2Id = sessionDto.User2Id,
                Mode = sessionDto.Mode,
                StartTime = sessionDto.StartTime == default
                             ? DateTime.UtcNow
                             : sessionDto.StartTime,
                EndTime = sessionDto.EndTime,
                WonUserId = sessionDto.WonUserId,
                FinalWave = sessionDto.FinalWave,
                TimePlayed = sessionDto.TimePlayed
            };

            await _gameSessionRepository.AddAsync(session);
            return session.SessionId;
        }

        /// <inheritdoc/>
        public async Task UpdateSessionAsync(GameSessionDTO sessionDto)
        {
            var session = await _gameSessionRepository.GetByIdAsync(sessionDto.SessionId);
            if (session == null)
                throw new Exception($"GameSession with ID {sessionDto.SessionId} not found.");

            session.User1Id = sessionDto.User1Id;
            session.User2Id = sessionDto.User2Id;
            session.Mode = sessionDto.Mode;
            session.StartTime = sessionDto.StartTime;
            session.EndTime = sessionDto.EndTime;
            session.WonUserId = sessionDto.WonUserId;
            session.FinalWave = sessionDto.FinalWave;
            session.TimePlayed = sessionDto.TimePlayed;

            await _gameSessionRepository.UpdateAsync(session);
        }

        /// <inheritdoc/>
        public async Task DeleteSessionAsync(int sessionId)
        {
            var session = await _gameSessionRepository.GetByIdAsync(sessionId);
            if (session == null)
                throw new Exception($"GameSession with ID {sessionId} not found.");

            await _gameSessionRepository.DeleteAsync(session);
        }

        /// <inheritdoc/>
        public async Task EndSessionAsync(int sessionId, int? wonUserId, int? finalWave, int? timePlayed)
        {
            var session = await _gameSessionRepository.GetByIdAsync(sessionId);
            if (session == null)
                throw new Exception($"GameSession with ID {sessionId} not found.");

            session.EndTime = DateTime.UtcNow;
            session.WonUserId = wonUserId;
            session.FinalWave = finalWave;
            session.TimePlayed = timePlayed;

            await _gameSessionRepository.UpdateAsync(session);
        }

        // -----------------------------------------------------------------
        // PRIVATE HELPER
        // -----------------------------------------------------------------

        /// <summary>
        /// Maps a <see cref="GameSession"/> entity to a <see cref="GameSessionDTO"/>.
        /// </summary>
        private GameSessionDTO MapToDTO(GameSession entity)
        {
            return new GameSessionDTO
            {
                SessionId = entity.SessionId,
                User1Id = entity.User1Id,
                User2Id = entity.User2Id,
                Mode = entity.Mode,
                StartTime = entity.StartTime,
                EndTime = entity.EndTime,
                WonUserId = entity.WonUserId,
                FinalWave = entity.FinalWave,
                TimePlayed = entity.TimePlayed
            };
        }
    }
}

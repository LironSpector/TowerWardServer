using DTOs;

namespace Services
{
    public interface IGameSessionService
    {
        Task<GameSessionDTO> GetSessionByIdAsync(int sessionId);
        Task<IEnumerable<GameSessionDTO>> GetAllSessionsAsync();
        Task<int> CreateSessionAsync(GameSessionDTO sessionDto);
        Task UpdateSessionAsync(GameSessionDTO sessionDto);
        Task DeleteSessionAsync(int sessionId);

        // Additional business logic:
        Task EndSessionAsync(int sessionId, int? wonUserId, int? finalWave, int? timePlayed);
    }
}

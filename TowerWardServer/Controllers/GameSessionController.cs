//Game sessions might be mostly created internally when a match starts, updated when it ends, etc.
//The client might only need read access to check their match history or the current session status.
using Microsoft.AspNetCore.Mvc;
using DTOs;
using Services;

namespace Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GameSessionController : ControllerBase
    {
        private readonly IGameSessionService _gameSessionService;

        public GameSessionController(IGameSessionService gameSessionService)
        {
            _gameSessionService = gameSessionService;
        }

        /// <summary>
        /// Get session by ID (e.g. for match history).
        /// </summary>
        [HttpGet("{sessionId}")]
        public async Task<IActionResult> GetSession(int sessionId)
        {
            var sessionDto = await _gameSessionService.GetSessionByIdAsync(sessionId);
            if (sessionDto == null) return NotFound();
            return Ok(sessionDto);
        }

        /// <summary>
        /// Get all sessions (admin or for debugging).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllSessions()
        {
            var sessions = await _gameSessionService.GetAllSessionsAsync();
            return Ok(sessions);
        }

        /// <summary>
        /// (Server) Create a new session. 
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateSession([FromBody] GameSessionDTO sessionDto)
        {
            try
            {
                // Typically, the server sets startTime, user1Id, user2Id, etc.
                var newSessionId = await _gameSessionService.CreateSessionAsync(sessionDto);
                return CreatedAtAction(nameof(GetSession), new { sessionId = newSessionId }, new { sessionId = newSessionId });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// (Server) Updates a session record. 
        /// (Typically used if you want to adjust the session in mid-game.)
        /// </summary>
        [HttpPut("{sessionId}")]
        public async Task<IActionResult> UpdateSession(int sessionId, [FromBody] GameSessionDTO sessionDto)
        {
            if (sessionId != sessionDto.SessionId) return BadRequest("Mismatched session ID");
            try
            {
                await _gameSessionService.UpdateSessionAsync(sessionDto);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// (Server) End a session, marking who won, final wave, etc.
        /// Optionally done automatically on "Game Over".
        /// </summary>
        [HttpPost("{sessionId}/end")]
        public async Task<IActionResult> EndSession(int sessionId, int? wonUserId, int? finalWave, int? timePlayed)
        {
            try
            {
                await _gameSessionService.EndSessionAsync(sessionId, wonUserId, finalWave, timePlayed);
                return Ok("Session ended successfully.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// (Admin) Delete a session record (rarely needed).
        /// </summary>
        [HttpDelete("{sessionId}")]
        public async Task<IActionResult> DeleteSession(int sessionId)
        {
            try
            {
                await _gameSessionService.DeleteSessionAsync(sessionId);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}

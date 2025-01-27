//For user stats, you may decide to make them mostly read-only for normal players (the server updates them after each match).
//But you can provide a read endpoint for the user to see their stats.
//Note: In a production environment, you’d secure these “server-only” endpoints so that players cannot just call them and give themselves XP or wins.
using Microsoft.AspNetCore.Mvc;
using DTOs;
using Services;

namespace Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserGameStatsController : ControllerBase
    {
        private readonly IUserGameStatsService _userGameStatsService;

        public UserGameStatsController(IUserGameStatsService userGameStatsService)
        {
            _userGameStatsService = userGameStatsService;
        }

        /// <summary>
        /// Get stats for a specific user.
        /// </summary>
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetStatsByUserId(int userId)
        {
            var statsDto = await _userGameStatsService.GetStatsByUserIdAsync(userId);
            if (statsDto == null) return NotFound();
            return Ok(statsDto);
        }

        // Typically, creation of user stats is done internally in the UserService (create user flow).
        // We can provide an endpoint if admin or server needs it:

        /// <summary>
        /// (Admin/Server) Create stats record for a user if they somehow don't have one.
        /// </summary>
        [HttpPost("{userId}")]
        public async Task<IActionResult> CreateStatsForUser(int userId)
        {
            try
            {
                await _userGameStatsService.CreateStatsForUserAsync(userId);
                return Ok("Stats created.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Update user stats (likely server-only).
        /// </summary>
        [HttpPut]
        public async Task<IActionResult> UpdateStats([FromBody] UserGameStatsDTO statsDto)
        {
            try
            {
                await _userGameStatsService.UpdateStatsAsync(statsDto);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Delete user stats (admin action).
        /// </summary>
        [HttpDelete("{userId}")]
        public async Task<IActionResult> DeleteStats(int userId)
        {
            try
            {
                await _userGameStatsService.DeleteStatsAsync(userId);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // Additional endpoints for adding XP or incrementing stats can be done if you want manual calls:

        /// <summary>
        /// (Server) Add XP to a user.
        /// </summary>
        [HttpPost("{userId}/add-xp/{amount}")]
        public async Task<IActionResult> AddXp(int userId, int amount)
        {
            try
            {
                await _userGameStatsService.AddXpAsync(userId, amount);
                return Ok($"Added {amount} XP to user {userId}.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// (Server) Increment games played for a user, e.g. at match end.
        /// </summary>
        [HttpPost("{userId}/increment-games")]
        public async Task<IActionResult> IncrementGames(int userId, bool won, bool singlePlayer)
        {
            try
            {
                await _userGameStatsService.IncrementGamesPlayedAsync(userId, won, singlePlayer);
                return Ok("Games played incremented.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}

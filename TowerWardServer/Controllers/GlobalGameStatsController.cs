//Global stats might be read-only to the public (“Show me how many total users or total games exist”).
//The server might call an internal method to increment them after each user registers or each match finishes.
using Microsoft.AspNetCore.Mvc;
using DTOs;
using Services;

namespace Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GlobalGameStatsController : ControllerBase
    {
        private readonly IGlobalGameStatsService _globalStatsService;

        public GlobalGameStatsController(IGlobalGameStatsService globalStatsService)
        {
            _globalStatsService = globalStatsService;
        }

        /// <summary>
        /// (Public/Client) Get global stats by ID (often 1).
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetGlobalStats(int id)
        {
            var stats = await _globalStatsService.GetGlobalStatsAsync(id);
            if (stats == null) return NotFound();
            return Ok(stats);
        }

        /// <summary>
        /// (Admin) Create a global stats record if it doesn't exist.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateGlobalStats([FromBody] GlobalGameStatsDTO statsDto)
        {
            try
            {
                await _globalStatsService.CreateGlobalStatsAsync(statsDto);
                return CreatedAtAction(nameof(GetGlobalStats), new { id = statsDto.Id }, statsDto);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// (Admin) Update global stats. 
        /// Possibly only used if you want to do a manual fix/override.
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateGlobalStats(int id, [FromBody] GlobalGameStatsDTO statsDto)
        {
            if (id != statsDto.Id) return BadRequest("Mismatched ID");
            try
            {
                await _globalStatsService.UpdateGlobalStatsAsync(statsDto);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// (Admin) Delete global stats. Typically never done.
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGlobalStats(int id)
        {
            try
            {
                await _globalStatsService.DeleteGlobalStatsAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// (Server) Increment total users by the specified amount.
        /// </summary>
        [HttpPost("{id}/increment-users/{amount}")]
        public async Task<IActionResult> IncrementTotalUsers(int id, int amount)
        {
            try
            {
                await _globalStatsService.IncrementTotalUsersAsync(id, amount);
                return Ok($"Incremented total users by {amount}.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// (Server) Increment games played, single or multi.
        /// </summary>
        [HttpPost("{id}/increment-games")]
        public async Task<IActionResult> IncrementGamesPlayed(int id, bool singlePlayer)
        {
            try
            {
                await _globalStatsService.IncrementGamesPlayedAsync(id, singlePlayer);
                return Ok("Incremented games played.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using DTOs;
using Services;
using System.Runtime.InteropServices;

namespace Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Creates a new user (registration).
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDTO createDto)
        {
            try
            {
                var newUserId = await _userService.CreateUserAsync(createDto);
                return CreatedAtAction(nameof(GetUserById), new { userId = newUserId }, new { userId = newUserId });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Gets user info by ID.
        /// </summary>
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUserById(int userId)
        {
            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null) return NotFound();
            return Ok(user);
        }

        /// <summary>
        /// Gets all users. (Potentially restricted to admins.)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userService.GetAllUsersAsync();
            return Ok(users);
        }

        /// <summary>
        /// Updates user info (e.g., avatar, status). 
        /// No password changes here.
        /// </summary>
        [HttpPut("{userId}")]
        public async Task<IActionResult> UpdateUser(int userId, [FromBody] UserDTO userDto)
        {
            if (userId != userDto.UserId) return BadRequest("Mismatched user ID");
            try
            {
                await _userService.UpdateUserAsync(userDto);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Deletes a user (potentially an admin action).
        /// </summary>
        [HttpDelete("{userId}")]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            try
            {
                await _userService.DeleteUserAsync(userId);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Ban a user (admin-only).
        /// </summary>
        [HttpPost("{userId}/ban")]
        public async Task<IActionResult> BanUser(int userId)
        {
            try
            {
                await _userService.BanUserAsync(userId);
                return Ok("User banned successfully.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Unban a user (admin-only).
        /// </summary>
        [HttpPost("{userId}/unban")]
        public async Task<IActionResult> UnbanUser(int userId)
        {
            try
            {
                await _userService.UnbanUserAsync(userId);
                return Ok("User unbanned successfully.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Update last login timestamp (maybe done after successful login).
        /// </summary>
        [HttpPost("{userId}/lastlogin")]
        public async Task<IActionResult> UpdateLastLogin(int userId)
        {
            try
            {
                await _userService.UpdateLastLoginAsync(userId);
                return Ok("Last login updated.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}

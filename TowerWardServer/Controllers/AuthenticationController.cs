using Microsoft.AspNetCore.Mvc;
using DTOs;
using Services;

namespace GameSolution.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthenticationController : ControllerBase
    {
        private readonly IAuthenticationService _authService;

        public AuthenticationController(IAuthenticationService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Logs in with username + password, returns an Access Token + Refresh Token if successful.
        /// </summary>
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDTO>> Login([FromBody] LoginDTO loginDto)
        {
            var response = await _authService.LoginAsync(loginDto.Username, loginDto.Password);
            if (response == null)
            {
                return Unauthorized(); // Unauthorized("Invalid or expired refresh token.");
            }
            return Ok(response);
        }

        /// <summary>
        /// Refreshes the access token using a refresh token.
        /// </summary>
        [HttpPost("refresh")]
        public async Task<ActionResult<AuthResponseDTO>> Refresh([FromBody] RefreshRequestDTO requestDto)
        {
            var response = await _authService.RefreshAsync(requestDto.RefreshToken);
            if (response == null)
            {
                return Unauthorized(); // Unauthorized("Invalid or expired refresh token.");
            }
            return Ok(response);
        }

        /// <summary>
        /// Revoke all refresh tokens for a given user ID (logout from all sessions).
        /// Typically requires admin or same user authentication.
        /// </summary>
        [HttpPost("revokeAll/{userId}")]
        public async Task<IActionResult> RevokeAllTokens(int userId)
        {
            // In real scenario, ensure the caller is authorized to do this.
            await _authService.RevokeAllAsync(userId);
            return Ok("All tokens revoked.");
        }
    }
}

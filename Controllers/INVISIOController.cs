using INVISIO.Models;
using INVISIO.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace INVISIO.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class INVISIOController : ControllerBase
    {
        private readonly INVISIOService _authService;
        private readonly BlacklistService _blacklistService;

        public INVISIOController(INVISIOService authService, BlacklistService blacklistService)
        {
            _authService = authService;
            _blacklistService = blacklistService;
        }

        [HttpPost("signup")]
        public async Task<IActionResult> Signup([FromBody] UserSignupDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(); 

            var user = await _authService.RegisterUserAsync(dto.FullName, dto.Email, dto.Password, dto.CompanyName);

            if (user == null)
                return Conflict(new { code = 4003, message = "User with this email already exists." });

            return Ok(new
            {
                code = 2000,
                message = "User registered successfully.",
                userId = user.Id
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(); 

            var token = await _authService.AuthenticateAsync(dto.Email, dto.Password);
            if (token == null)
                return Unauthorized(new { code = 5005, message = "Invalid credentials." });

            return Ok(new { code = 2000, token });
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

            if (!string.IsNullOrWhiteSpace(token))
            {
                var jwtHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var jwt = jwtHandler.ReadJwtToken(token);
                var expiry = jwt.ValidTo;

                await _blacklistService.AddToBlacklistAsync(token, expiry);
            }

            return Ok(new { code = 2000, message = "Logged out successfully." });
        }

        [HttpGet("getMe")] 
        [Authorize]
        public IActionResult GetProfile()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var fullName = User.FindFirst(ClaimTypes.Name)?.Value;
            var companyName = User.FindFirst("CompanyName")?.Value;


            return Ok(new
            {
                code = 2000,
                message = "Access granted.",
                user = new
                {
                    id = userId,
                    email,
                    fullName,
                    companyName 
                }
            });
        }
    }
}
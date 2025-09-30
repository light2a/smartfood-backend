using BLL.IServices;
using Microsoft.AspNetCore.Mvc;
using SmartFoodAPI.DTOs.Auth;
using System;
using System.Threading.Tasks;

namespace SmartFoodAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var token = await _authService.LoginAsync(request.Email, request.Password);
            if (token == null)
                return Unauthorized(new { message = "Invalid credentials or inactive account." });

            return Ok(new LoginResponse { Token = token });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var account = await _authService.RegisterAsync(request.FullName, request.Email, request.Password);
                return Ok(new RegisterResponse
                {
                    Message = "Registration successful",
                    AccountId = account.AccountId
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}

using BLL.IServices;
using Microsoft.AspNetCore.Mvc;
using SmartFoodAPI.DTOs.Auth;
using System;
using System.Text.RegularExpressions;
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
                // 1. Validate required fields
                if (string.IsNullOrWhiteSpace(request.FullName) ||
                    string.IsNullOrWhiteSpace(request.Email) ||
                    string.IsNullOrWhiteSpace(request.Password) ||
                    string.IsNullOrWhiteSpace(request.ConfirmPassword))
                {
                    return BadRequest(new { error = "All fields are required." });
                }

                // 2. Validate email format
                var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                if (!emailRegex.IsMatch(request.Email))
                    return BadRequest(new { error = "Invalid email format." });

                // 3. Validate password complexity (example: min 6 chars, at least 1 digit)
                var passwordRegex = new Regex(@"^(?=.*\d).{6,}$");
                if (!passwordRegex.IsMatch(request.Password))
                    return BadRequest(new { error = "Password must be at least 6 characters and contain at least one number." });

                // 4. Confirm password matches
                if (request.Password != request.ConfirmPassword)
                    return BadRequest(new { error = "Password and confirmation do not match." });

                // 5. Optional: validate phone number format
                if (!string.IsNullOrEmpty(request.PhoneNumber))
                {
                    var phoneRegex = new Regex(@"^\+?\d{10,15}$"); // simple international format
                    if (!phoneRegex.IsMatch(request.PhoneNumber))
                        return BadRequest(new { error = "Invalid phone number format." });
                }

                // Call service to register
                var account = await _authService.RegisterAsync(
                    request.FullName,
                    request.Email,
                    request.Password,
                    request.PhoneNumber
                );

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
        [HttpPost("register-seller")]
        public async Task<IActionResult> RegisterSeller([FromBody] RegisterSellerRequest request)
        {
            try
            {
                // 1. Validate required fields
                if (string.IsNullOrWhiteSpace(request.FullName) ||
                    string.IsNullOrWhiteSpace(request.Email) ||
                    string.IsNullOrWhiteSpace(request.Password) ||
                    string.IsNullOrWhiteSpace(request.ConfirmPassword)) 
                {
                    return BadRequest(new { error = "All fields are required." });
                }

                // 2. Validate email format
                var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                if (!emailRegex.IsMatch(request.Email))
                    return BadRequest(new { error = "Invalid email format." });

                // 3. Validate password
                var passwordRegex = new Regex(@"^(?=.*\d).{6,}$");
                if (!passwordRegex.IsMatch(request.Password))
                    return BadRequest(new { error = "Password must be at least 6 characters and contain at least one number." });

                if (request.Password != request.ConfirmPassword)
                    return BadRequest(new { error = "Password and confirmation do not match." });

                // 4. Optional phone validation
                if (!string.IsNullOrEmpty(request.PhoneNumber))
                {
                    var phoneRegex = new Regex(@"^\+?\d{10,15}$");
                    if (!phoneRegex.IsMatch(request.PhoneNumber))
                        return BadRequest(new { error = "Invalid phone number format." });
                }

                // 5. Call service to register seller
                var account = await _authService.RegisterSellerAsync(
                    request.FullName,
                    request.Email,
                    request.Password,
                    request.PhoneNumber
                );

                return Ok(new RegisterResponse
                {
                    Message = "Seller registration successful. Pending admin approval.",
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

using BLL.IServices;
using DAL.Models;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using SmartFoodAPI.DTOs.Auth;
using System;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SmartFoodAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;
        private readonly IConfiguration _configuration;

        public AuthController(IAuthService authService, ILogger<AuthController> logger, IConfiguration configuration)
        {
            _authService = authService;
            _logger = logger;
            _configuration = configuration;
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
        [HttpGet("google-login")]
        public IActionResult GoogleLogin(string returnUrl = "/")
        {
            try
            {
                // Generate and store state with additional security
                var state = Guid.NewGuid().ToString("N");
                HttpContext.Session.SetString("OAuthState", state);

                // Also store the return URL
                HttpContext.Session.SetString("OAuthReturnUrl", returnUrl);

                _logger.LogInformation("Initiating Google OAuth with state: {State}", state);

                var properties = new AuthenticationProperties
                {
                    RedirectUri = Url.Action("GoogleResponse", "Auth"),
                    Items =
            {
                { "LoginProvider", "Google" },
                { "state", state }
            }
                };

                return Challenge(properties, GoogleDefaults.AuthenticationScheme);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating Google login");
                return BadRequest(new { error = "Failed to initiate Google login" });
            }
        }

        [HttpGet("google-response")]
        public async Task<IActionResult> GoogleResponse()
        {
            try
            {
                _logger.LogInformation("Processing Google OAuth callback: {Query}", Request.QueryString);

                // 1️⃣ Handle empty second redirect from Google gracefully
                if (string.IsNullOrEmpty(Request.Query["code"]) &&
                    string.IsNullOrEmpty(Request.Query["state"]) &&
                    string.IsNullOrEmpty(Request.Query["error"]))
                {
                    _logger.LogWarning("[GoogleOAuth] Duplicate empty callback detected. Returning simple JSON instead of 404.");
                    return new JsonResult(new
                    {
                        message = "Duplicate Google callback ignored (no state or code).",
                        handled = true
                    });
                }

                // 2️⃣ Handle explicit OAuth error (e.g., user canceled)
                var error = Request.Query["error"].ToString();
                if (!string.IsNullOrEmpty(error))
                {
                    _logger.LogError("[GoogleOAuth] OAuth error: {Error} | Description: {Description}",
                        error, Request.Query["error_description"]);
                    return Redirect($"{_configuration["Frontend:BaseUrl"]}/auth/failed?error=google_error&message={error}");
                }

                // 3️⃣ Validate OAuth parameters
                var code = Request.Query["code"].ToString();
                var returnedState = Request.Query["state"].ToString();

                if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(returnedState))
                {
                    _logger.LogWarning("[GoogleOAuth] Missing code or state in callback");
                    return Redirect($"{_configuration["Frontend:BaseUrl"]}/auth/failed?error=missing_parameters");
                }

                var savedState = HttpContext.Session.GetString("OAuthState");
                var returnUrl = HttpContext.Session.GetString("OAuthReturnUrl") ?? "/";

                if (string.IsNullOrEmpty(savedState) || savedState != returnedState)
                {
                    _logger.LogError("[GoogleOAuth] State mismatch: saved={Saved}, returned={Returned}",
                        savedState, returnedState);
                    HttpContext.Session.Remove("OAuthState");
                    HttpContext.Session.Remove("OAuthReturnUrl");
                    return Redirect($"{_configuration["Frontend:BaseUrl"]}/auth/failed?error=invalid_state");
                }

                // 4️⃣ Authenticate with Google’s cookie
                var result = await HttpContext.AuthenticateAsync("ExternalCookie");
                if (!result.Succeeded || result.Principal == null)
                {
                    _logger.LogWarning("[GoogleOAuth] External auth failed: {Failure}", result.Failure?.Message);
                    return Redirect($"{_configuration["Frontend:BaseUrl"]}/auth/failed?error=auth_failed");
                }

                // 5️⃣ Handle Google identity
                return await ProcessGoogleAuthentication(result.Principal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GoogleOAuth] Unexpected error");
                await HttpContext.SignOutAsync("ExternalCookie");
                HttpContext.Session.Clear();

                // Fallback: return JSON if frontend not reachable
                if (HttpContext.Request.Headers["Accept"].ToString().Contains("application/json"))
                {
                    return new JsonResult(new
                    {
                        error = "unexpected_error",
                        message = ex.Message
                    });
                }

                return Redirect($"{_configuration["Frontend:BaseUrl"]}/auth/failed?error=unexpected_error");
            }
        }


        private async Task<IActionResult> ProcessGoogleAuthentication(ClaimsPrincipal externalUser)
        {
            var email = externalUser.FindFirst(ClaimTypes.Email)?.Value;
            var name = externalUser.FindFirst(ClaimTypes.Name)?.Value;
            var googleId = externalUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            _logger.LogInformation("[GoogleOAuth] Authenticated user: {Email}, ID: {GoogleId}", email, googleId);

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(googleId))
                return Redirect($"{_configuration["Frontend:BaseUrl"]}/auth/failed?error=missing_claims");

            var account = await _authService.HandleExternalLoginAsync(email, name, "Google", googleId);
            var token = await _authService.GenerateJwtTokenAsync(account);

            await HttpContext.SignOutAsync("ExternalCookie");
            HttpContext.Session.Clear();

            var redirectUrl = $"{_configuration["Frontend:BaseUrl"]}/auth/success" +
                              $"?token={Uri.EscapeDataString(token)}" +
                              $"&accountId={account.AccountId}" +
                              $"&email={Uri.EscapeDataString(account.Email)}" +
                              $"&role={Uri.EscapeDataString(account.Role?.RoleName ?? "Customer")}";

            _logger.LogInformation("[GoogleOAuth] Redirecting to: {RedirectUrl}", redirectUrl);
            return Redirect(redirectUrl);
        }

    }
}

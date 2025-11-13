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
using BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Stripe;
using Stripe.FinancialConnections;
using SmartFoodAPI.Common;

namespace SmartFoodAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;

        public AuthController(IAuthService authService, ILogger<AuthController> logger, IConfiguration configuration, IEmailService emailService)
        {
            _authService = authService;
            _logger = logger;
            _configuration = configuration;
            _emailService = emailService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] DTOs.Auth.LoginRequest request)
        {
            try
            {
                var token = await _authService.LoginAsync(request.Email, request.Password);
                if (token == null)
                    return Unauthorized(new { message = "Email hoặc mật khẩu không đúng." });

                return Ok(new { token });
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("cấm"))
                    return BadRequest(new { message = ex.Message });

                return StatusCode(500, ErrorResponse.FromStatus(500, $"Server error: {ex.Message}"));
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] DTOs.Auth.RegisterRequest request)
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
            if (!ModelState.IsValid)
                return BadRequest(ModelState); // ✅ validation handled by attributes

            try
            {
                // 1️⃣ Register seller (inactive by default)
                var account = await _authService.RegisterSellerAsync(
                    request.FullName,
                    request.Email,
                    request.Password,
                    request.PhoneNumber
                );

                // 2️⃣ Generate OTP (6-digit)
                var otp = new Random().Next(100000, 999999).ToString();
                var expiration = DateTime.UtcNow.AddMinutes(5);

                await _authService.SaveOtpAsync(request.Email, otp, expiration);

                // 3️⃣ Send OTP email
                await _emailService.SendOtpEmailAsync(request.Email, otp);

                return Ok(new
                {
                    message = "Seller registration successful. Please verify your email using the OTP sent to your inbox.",
                    accountId = account.AccountId
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("otp/register")]
        public async Task<IActionResult> OtpRegister([FromBody] OtpRegistrationRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                                              .Select(e => e.ErrorMessage)
                                              .ToList();
                return BadRequest(new { errors });
            }

            if (request.Password != request.ConfirmPassword)
                return BadRequest(new { error = "Password and Confirm Password do not match." });

            DAL.Models.Account account;
            try
            {
                account = await _authService.RegisterAsync(
                    request.FullName,
                    request.Email,
                    request.Password,
                    request.PhoneNumber
                );
            }
            catch (Exception ex)
            {
                return Conflict(new { error = ex.Message });
            }

            var otpCode = new Random().Next(100000, 999999).ToString();
            var expiration = DateTime.UtcNow.AddMinutes(5);

            bool otpSaved = await _authService.SaveOtpAsync(request.Email, otpCode, expiration);
            if (!otpSaved)
                return StatusCode(500, "Failed to generate OTP.");

            try
            {
                await _emailService.SendOtpEmailAsync(request.Email, otpCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send OTP email to {Email}", request.Email);
                return StatusCode(500, "Failed to send OTP email.");
            }

            return Ok(new
            {
                message = "Registration successful. An OTP has been sent to your email. Please verify to activate your account."
            });
        }
        [HttpPost("otp/verify")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                                              .Select(e => e.ErrorMessage)
                                              .ToList();
                return BadRequest(new { errors });
            }
            var account = await _authService.GetAccountByEmailAsync(request.Email);
            if (account == null)
                return NotFound("Account not found.");
            if (account.IsActive == true)
                return BadRequest("Account is already active.");

            bool isValid = await _authService.VerifyOtpAsync(request.Email, request.OtpCode);
            if (!isValid)
                return BadRequest("Invalid or expired OTP.");
            account.IsActive = true;
            await _authService.UpdateAccountAsync(account);

            _logger.LogInformation("OTP verification successful, account updated.");
            await _authService.InvalidateOtpAsync(request.Email);

            return Ok(new { message = "Account verified successfully." });
        }

        [HttpPost("otp/resend")]
        public async Task<IActionResult> ResendOtp([FromBody] ResendOtpRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                                              .Select(e => e.ErrorMessage)
                                              .ToList();
                return BadRequest(new { errors });
            }

            var account = await _authService.GetAccountByEmailAsync(request.Email);
            if (account == null)
                return NotFound("Account not found.");

            if (account.IsActive == true)
                return Ok(new { message = "Account is already verified." });

            var currentOtp = await _authService.GetCurrentOtpAsync(request.Email);
            if (currentOtp != null && currentOtp.Expiration > DateTime.UtcNow)
            {
                return Ok(new { message = "Your OTP is still active. Please use the existing OTP." });
            }

            var otpCode = new Random().Next(100000, 999999).ToString();
            var expiration = DateTime.UtcNow.AddMinutes(5);

            bool otpSaved = await _authService.SaveOtpAsync(request.Email, otpCode, expiration);
            if (!otpSaved)
                return StatusCode(500, "Failed to generate OTP.");

            try
            {
                await _emailService.SendOtpEmailAsync(request.Email, otpCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resend OTP email to {Email}", request.Email);
                return StatusCode(500, "Failed to resend OTP email.");
            }

            return Ok(new { message = "A new OTP has been sent to your email." });
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

        [Authorize]
        [HttpPut("update-account")]
        public async Task<IActionResult> UpdateAccount([FromBody] UpdateAccountRequest request)
        {
            try
            {
                // ✅ Get the current user's ID from JWT claims
                var accountIdClaim = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
                if (accountIdClaim == null)
                    return Unauthorized(new { error = "Invalid token or missing account ID." });

                int accountId = int.Parse(accountIdClaim);

                // ✅ Update account using the ID from token
                var account = await _authService.UpdateAccountAsync(accountId, request.FullName, request.PhoneNumber);

                return Ok(new { message = "Account updated successfully", account });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }


        [Authorize]
        [HttpDelete("deactivate-account")]
        public async Task<IActionResult> DeactivateAccount()
        {
            try
            {
                var accountIdClaim = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
                if (accountIdClaim == null)
                    return Unauthorized(new { error = "Invalid token or missing account ID." });

                int accountId = int.Parse(accountIdClaim);

                var account = await _authService.DeactivateAccountAsync(accountId);

                return Ok(new { message = "Account deactivated successfully", account });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var account = await _authService.GetAccountByEmailAsync(request.Email);
            if (account == null)
            {
                return Ok(new { message = "If an account with that email exists, you will receive a password reset email." });
            }

            var token = Guid.NewGuid().ToString();
            var expiration = DateTime.UtcNow.AddHours(1);
            await _authService.SavePasswordResetTokenAsync(account.AccountId, token, expiration);

            await _emailService.SendPasswordResetEmailAsync(request.Email, token);

            return Ok(new { message = "If an account with that email exists, you will receive a password reset email." });
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] DTOs.Auth.ResetPasswordRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var account = await _authService.GetAccountByEmailAsync(request.Email);
            if (account == null)
                return BadRequest("Invalid request.");
            var tokenValid = await _authService.VerifyPasswordResetTokenAsync(account.AccountId, request.Token);
            if (!tokenValid)
                return BadRequest("Invalid or expired token.");
            account.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _authService.UpdateAccountAsync(account);
            await _authService.InvalidatePasswordResetTokenAsync(account.AccountId, request.Token);
            return Ok(new { message = "Password reset successfully." });
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

        

        //[HttpPost("connect")]
        //[Authorize(Roles = "Seller")]
        //public async Task<IActionResult> CreateStripeAccountLink()
        //{
        //    var sellerId = int.Parse(User.FindFirst("SellerId") ?? throw new Exception("Unauthorized"));
        //    var seller = await _sellerRepository.GetByIdAsync(sellerId);
        //    if (seller == null)
        //        return NotFound("Seller not found");

        //    StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];

        //    // Create a Connect account if none exists
        //    if (string.IsNullOrEmpty(seller.StripeAccountId))
        //    {
        //        var accountService = new AccountService();
        //        var account = await accountService.CreateAsync(new AccountCreateOptions
        //        {
        //            Type = "express",
        //            Email = seller.User.Email
        //        });

        //        seller.StripeAccountId = account.Id;
        //        await _sellerRepository.UpdateAsync(seller);
        //    }

        //    // Create an onboarding link
        //    var accountLinkService = new AccountLinkService();
        //    var link = await accountLinkService.CreateAsync(new AccountLinkCreateOptions
        //    {
        //        Account = seller.StripeAccountId,
        //        RefreshUrl = "https://your-frontend.com/seller/stripe/refresh",
        //        ReturnUrl = "https://your-frontend.com/seller/stripe/complete",
        //        Type = "account_onboarding"
        //    });

        //    return Ok(new { url = link.Url });
        //}

    }
}

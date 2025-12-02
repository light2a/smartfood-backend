using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BLL.IServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BLL.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _resendApiKey;
        private readonly string _senderEmail;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _resendApiKey = _configuration["Resend:ApiKey"];
            if (string.IsNullOrEmpty(_resendApiKey))
            {
                throw new InvalidOperationException("Resend API key is not configured.");
            }
            _senderEmail = "support@smartfood.dpdns.org";
        }

        private async Task SendEmailAsync(string email, string subject, string htmlBody, string plainTextBody)
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _resendApiKey);

            var payload = new
            {
                from = _senderEmail,
                to = email,
                subject = subject,
                html = htmlBody,
                text = plainTextBody,
                // Disable click tracking and open tracking
                enable_tracking = false,
                enable_performance_tracking = false
            };

            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync("https://api.resend.com/emails", content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully sent email to {Email} via Resend API.", email);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to send email to {Email} via Resend API. Status: {StatusCode}, Response: {ErrorContent}", email, response.StatusCode, errorContent);
                throw new Exception($"Failure sending mail: {errorContent}");
            }
        }

        public async Task SendPasswordResetEmailAsync(string email, string token)
        {
            try
            {
                _logger.LogInformation("Attempting to send password reset email to {Email} via Resend API", email);

                string baseUrl = _configuration["Frontend:ResetPasswordUrl"];
                string frontendLink = $"{baseUrl}?email={email}&token={token}";
                string htmlBody = $@"<p>Please reset your password by clicking this link:</p>
                                   <a href='{frontendLink}'>Reset Password</a>";
                string plainTextBody = $"Please reset your password by clicking this link: {frontendLink}";

                await SendEmailAsync(email, "Reset Your Password", htmlBody, plainTextBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email to {Email}.", email);
                throw;
            }
        }

        public async Task SendOtpEmailAsync(string email, string otp)
        {
            try
            {
                _logger.LogInformation("Attempting to send OTP email to {Email} via Resend API", email);

                string htmlBody = $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Your OTP Code</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ width: 90%; max-width: 600px; margin: 20px auto; padding: 20px; border: 1px solid #ddd; border-radius: 10px; }}
        .header {{ font-size: 24px; font-weight: bold; color: #444; text-align: center; margin-bottom: 20px; }}
        .otp-code {{ font-size: 28px; font-weight: bold; color: #0056b3; text-align: center; margin: 20px 0; letter-spacing: 4px; }}
        .footer {{ margin-top: 20px; font-size: 12px; color: #777; text-align: center; }}
        .warning {{ font-size: 14px; color: #c00; text-align: center; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">SmartFood Verification</div>
        <p>Hello,</p>
        <p>Thank you for registering with SmartFood. Please use the following One-Time Password (OTP) to verify your account. The code is valid for 5 minutes.</p>
        <div class=""otp-code"">{otp}</div>
        <p class=""warning"">For your security, please do not share this code with anyone.</p>
        <p>If you did not request this, please ignore this email.</p>
        <br>
        <p>Best regards,<br>The SmartFood Team</p>
        <div class=""footer"">
            <p>&copy; 2025 SmartFood. All rights reserved.</p>
            <p>This is an automated message, please do not reply.</p>
        </div>
    </div>
</body>
</html>";
                string plainTextBody = $@"Hello,

Thank you for registering with SmartFood. Please use the following One-Time Password (OTP) to verify your account. The code is valid for 5 minutes.

Your OTP Code: {otp}

For your security, please do not share this code with anyone.

If you did not request this, please ignore this email.

Best regards,
The SmartFood Team

© 2025 SmartFood. All rights reserved.
This is an automated message, please do not reply.
";
                await SendEmailAsync(email, "Your OTP Code", htmlBody, plainTextBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send OTP email to {Email}.", email);
                throw;
            }
        }

        public async Task SendOtpReminderEmailAsync(string email)
        {
            Console.WriteLine($"Sending OTP reminder email to: {email}");
            await Task.CompletedTask;
        }
    }
}


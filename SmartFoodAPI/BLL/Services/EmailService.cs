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
            _senderEmail = "onboarding@resend.dev";
        }

        private async Task SendEmailAsync(string email, string subject, string htmlBody)
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _resendApiKey);

            var payload = new
            {
                from = _senderEmail,
                to = email,
                subject = subject,
                html = htmlBody
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

                await SendEmailAsync(email, "Reset Your Password", htmlBody);
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

                string htmlBody = $"Your OTP code is: <strong>{otp}</strong>. It is valid for 5 minutes.";
                await SendEmailAsync(email, "Your OTP Code", htmlBody);
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


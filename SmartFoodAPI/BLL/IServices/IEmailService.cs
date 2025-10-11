using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.IServices
{
    public interface IEmailService
    {
        Task SendPasswordResetEmailAsync(string email, string resetLink);
        Task SendOtpEmailAsync(string email, string otp);
        Task SendOtpReminderEmailAsync(string email);
    }
}

using BLL.DTOs.Payment;
using System.Threading.Tasks;

namespace BLL.IServices
{
    public interface IPaymentService
    {
        Task<string> CreatePayOsOrderAsync(int orderId);
        Task<bool> HandleCallbackAsync(PayOsWebhookDto callback);
        bool VerifySignature(PayOsWebhookDto callback);
    }
}

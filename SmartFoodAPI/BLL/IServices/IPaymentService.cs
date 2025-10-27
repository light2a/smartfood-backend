using Stripe;
using System.Threading.Tasks;

namespace BLL.IServices
{
    public interface IPaymentService
    {
        Task<string> CreatePaymentIntentAsync(int orderId);
        Task<bool> ConfirmPaymentAsync(string paymentIntentId);
        Task HandleStripePaymentSucceededAsync(PaymentIntent intent);
    }
}

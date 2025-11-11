using BLL.IServices;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using System.IO;
using System.Threading.Tasks;

namespace SmartFoodAPI.Controllers
{
    [ApiController]
    [Route("api/stripe/webhook")]
    public class StripeWebhookController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly ISellerService _sellerService;
        private readonly ILogger<StripeWebhookController> _logger;
        private readonly IConfiguration _config;

        public StripeWebhookController(
            IPaymentService paymentService,
            ISellerService sellerService,
            ILogger<StripeWebhookController> logger,
            IConfiguration config)
        {
            _paymentService = paymentService;
            _sellerService = sellerService;
            _logger = logger;
            _config = config;
        }

        [HttpPost]
        public async Task<IActionResult> HandleWebhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            var endpointSecret = _config["Stripe:WebhookSecret"]; // from Stripe dashboard

            Event stripeEvent;
            try
            {
                stripeEvent = EventUtility.ConstructEvent(
                    json,
                    Request.Headers["Stripe-Signature"],
                    endpointSecret
                );
            }
            catch (StripeException e)
            {
                _logger.LogError(e, "⚠️ Invalid Stripe webhook signature.");
                return BadRequest();
            }

            _logger.LogInformation("✅ Stripe event received: {Type}", stripeEvent.Type);

            switch (stripeEvent.Type)
            {
                // 💵 PAYMENT SUCCESS
                case "payment_intent.succeeded":
                    var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                    if (paymentIntent != null)
                    {
                        await _paymentService.HandleStripePaymentSucceededAsync(paymentIntent);
                    }
                    break;

                // 🏦 SELLER ONBOARDING COMPLETED
                case "account.updated":
                    var account = stripeEvent.Data.Object as Stripe.Account;
                    if (account != null && account.DetailsSubmitted)
                    {
                        await _sellerService.MarkStripeOnboardingCompletedAsync(account.Id);
                        _logger.LogInformation($"Seller {account.Id} onboarding marked complete.");
                    }
                    break;

                default:
                    _logger.LogInformation("Unhandled Stripe event type: {Type}", stripeEvent.Type);
                    break;
            }

            return Ok();
        }
    }
}

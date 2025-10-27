using BLL.IServices;
using DAL.IRepositories;
using DAL.Models;
using Microsoft.Extensions.Configuration;
using Stripe;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BLL.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ISellerRepository _sellerRepository;
        private readonly IConfiguration _config;

        public PaymentService(IOrderRepository orderRepository, ISellerRepository sellerRepository, IConfiguration config)
        {
            _orderRepository = orderRepository;
            _sellerRepository = sellerRepository;
            _config = config;

            StripeConfiguration.ApiKey = _config["Stripe:SecretKey"];
        }

        public async Task<string> CreatePaymentIntentAsync(int orderId)
        {
            var order = await _orderRepository.GetDetailByIdAsync(orderId);
            if (order == null)
                throw new Exception("Order not found.");

            var seller = await _sellerRepository.GetByIdAsync(order.Restaurant.SellerId);
            if (seller == null || string.IsNullOrEmpty(seller.StripeAccountId))
                throw new Exception("Seller not connected to Stripe.");

            // 💰 Calculate amounts
            var sellerAmount = order.FinalAmount * 0.8m;
            var platformAmount = order.FinalAmount * 0.2m;

            var totalInCents = (long)(order.FinalAmount * 100);
            var sellerAmountCents = (long)(sellerAmount * 100);
            var platformAmountCents = (long)(platformAmount * 100);

            // ✅ Create a PaymentIntent (funds go to the platform first)
            var options = new PaymentIntentCreateOptions
            {
                Amount = totalInCents,
                Currency = "usd", // or "vnd" if supported by your account
                Description = $"Payment for Order #{order.Id}",
                Metadata = new Dictionary<string, string>
                {
                    { "orderId", order.Id.ToString() },
                    { "sellerId", seller.Id.ToString() },
                    { "sellerAmount", sellerAmount.ToString() },
                    { "platformAmount", platformAmount.ToString() }
                },
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true,
                }
            };

            var service = new PaymentIntentService();
            var intent = await service.CreateAsync(options);

            return intent.ClientSecret;
        }

        public async Task<bool> ConfirmPaymentAsync(string paymentIntentId)
        {
            var service = new PaymentIntentService();
            var intent = await service.GetAsync(paymentIntentId);

            if (intent.Status != "succeeded")
                return false;

            // ✅ Retrieve metadata
            var orderId = int.Parse(intent.Metadata["orderId"]);
            var sellerId = int.Parse(intent.Metadata["sellerId"]);
            var sellerAmount = decimal.Parse(intent.Metadata["sellerAmount"]);

            // ✅ Get seller details
            var seller = await _sellerRepository.GetByIdAsync(sellerId);
            if (seller == null || string.IsNullOrEmpty(seller.StripeAccountId))
                throw new Exception("Seller Stripe account not found.");

            // ✅ Transfer 80% to seller's connected account
            var transferService = new TransferService();
            var transfer = await transferService.CreateAsync(new TransferCreateOptions
            {
                Amount = (long)(sellerAmount * 100),
                Currency = "usd",
                Destination = seller.StripeAccountId,
                TransferGroup = $"order_{orderId}",
                Metadata = new Dictionary<string, string>
                {
                    { "orderId", orderId.ToString() },
                    { "note", "Seller payout after successful customer payment" }
                }
            });

            // ✅ Mark order as Paid
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order != null)
            {
                order.StatusHistory.Add(new OrderStatusHistory
                {
                    OrderId = order.Id,
                    Status = "Paid",
                    Note = "Payment successful and seller credited via Stripe."
                });

                await _orderRepository.UpdateAsync(order);
            }

            return true;
        }

        public async Task HandleStripePaymentSucceededAsync(PaymentIntent intent)
        {
            var orderId = int.Parse(intent.Metadata["orderId"]);
            var sellerId = int.Parse(intent.Metadata["sellerId"]);
            var sellerAmount = decimal.Parse(intent.Metadata["sellerAmount"]);

            var order = await _orderRepository.GetByIdAsync(orderId);
            var seller = await _sellerRepository.GetByIdAsync(sellerId);

            if (order == null || seller == null)
                throw new Exception("Order or seller not found.");

            // ✅ Send transfer to seller
            var transferService = new TransferService();
            await transferService.CreateAsync(new TransferCreateOptions
            {
                Amount = (long)(sellerAmount * 100),
                Currency = "usd",
                Destination = seller.StripeAccountId,
                TransferGroup = $"order_{orderId}"
            });

            // ✅ Mark order as paid
            order.StatusHistory.Add(new OrderStatusHistory
            {
                OrderId = order.Id,
                Status = "Paid",
                Note = "Payment succeeded via Stripe webhook."
            });

            await _orderRepository.UpdateAsync(order);
        }

    }
}

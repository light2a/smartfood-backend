using BLL.DTOs.Payment;
using BLL.Helpers;
using BLL.IServices;
using DAL.IRepositories;
using DAL.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PayOS;
using PayOS.Crypto;
using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.V1.Payouts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

public class PaymentService : IPaymentService
{
    private readonly PayOSClient _client;
    private readonly IOrderRepository _orderRepo;
    private readonly ISellerRepository _sellerRepo;
    private readonly ILogger<PaymentService> _logger;
    private readonly IConfiguration _config;
    private readonly CryptoProvider _crypto;
    private readonly IServiceScopeFactory _scopeFactory;

    public PaymentService(
        IConfiguration config,
        IOrderRepository orderRepo,
        ISellerRepository sellerRepo,
        ILogger<PaymentService> logger,
        IServiceScopeFactory scopeFactory)
    {
        _config = config;
        _client = new PayOSClient(
            config["PayOS:ClientId"],
            config["PayOS:ApiKey"],
            config["PayOS:ChecksumKey"]
        );

        _orderRepo = orderRepo;
        _sellerRepo = sellerRepo;
        _logger = logger;
        _scopeFactory = scopeFactory;
        _crypto = new CryptoProvider();
    }

    public async Task<string> CreatePayOsOrderAsync(int orderId)
    {
        var order = await _orderRepo.GetDetailByIdAsync(orderId);
        if (order == null) throw new Exception("Order not found.");

        var request = new CreatePaymentLinkRequest
        {
            OrderCode = order.Id,
            Amount = (int)order.FinalAmount,
            Description = $"Payment for order #{order.Id}",
            ReturnUrl = $"{_config["PayOS:ReturnUrl"]}?orderId={order.Id}",
            CancelUrl = $"{_config["PayOS:CancelUrl"]}?orderId={order.Id}"
        };

        var result = await _client.PaymentRequests.CreateAsync(request);
        return result.CheckoutUrl;
    }

    public async Task<bool> HandleCallbackAsync(PayOsWebhookDto callback)
    {
        _logger.LogInformation($"📨 Processing webhook for orderCode: {callback.data.orderCode}");

        // ✅ Test webhook bypass (cho test data từ PayOS)
        if (callback.data.orderCode == 123)
        {
            _logger.LogInformation("⚠️ Test webhook detected (orderCode=123). Skipping signature verification and order processing.");
            return callback.code == "00";
        }

        // ✅ Verify signature cho production webhooks
        try
        {
            if (!VerifySignature(callback))
            {
                _logger.LogError($"❌ Signature verification FAILED for orderCode {callback.data.orderCode}");
                _logger.LogError($"Expected vs Received might not match. Check ChecksumKey.");
                return false;
            }
            _logger.LogInformation($"✅ Signature verified successfully for orderCode {callback.data.orderCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during signature verification");
            return false;
        }

        // ✅ Check payment status
        if (callback.code != "00")
        {
            _logger.LogError($"❌ Payment unsuccessful: {callback.code} - {callback.desc}");
            return false;
        }

        // ✅ Fetch order with null check
        Order order;
        try
        {
            order = await _orderRepo.GetDetailByIdAsync((int)callback.data.orderCode);
            if (order == null)
            {
                _logger.LogError($"❌ Order not found: {callback.data.orderCode}");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ Database error fetching order {callback.data.orderCode}");
            return false;
        }

        // ✅ Update order status
        try
        {
            order.StatusHistory.Add(new OrderStatusHistory
            {
                OrderId = order.Id,
                Status = "Paid",
                Note = $"PayOS reference: {callback.data.reference}",
                CreatedAt = DateTime.UtcNow
            });
            await _orderRepo.UpdateAsync(order);
            _logger.LogInformation($"✅ Order {order.Id} updated to 'Paid' status");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ Failed to update order {order.Id}");
            return false;
        }

        // ✅ Process payout asynchronously (không block webhook response)
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var sellerRepo = scope.ServiceProvider.GetRequiredService<ISellerRepository>();
                var orderRepo = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
                await ProcessPayoutAsync(order.Id, sellerRepo, orderRepo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Background payout task failed for order {order.Id}");
            }
        });

        return true;
    }

    private async Task ProcessPayoutAsync(int orderId, ISellerRepository sellerRepo, IOrderRepository orderRepo)
    {
        try
        {
            _logger.LogInformation($"💰 Starting payout process for order {orderId}");

            // Re-fetch order với scope mới
            var order = await orderRepo.GetDetailByIdAsync(orderId);
            if (order == null)
            {
                _logger.LogError($"❌ Order {orderId} not found in payout scope");
                return;
            }

            var seller = await sellerRepo.GetByIdAsync(order.Restaurant.SellerId);
            if (seller == null)
            {
                _logger.LogError($"❌ Seller not found for order {orderId}");
                return;
            }

            decimal sellerShare = order.FinalAmount * 0.8m;
            int payoutAmount = (int)sellerShare;

            _logger.LogInformation($"💰 Payout details: {sellerShare:N0} VND to {seller.BankCode} - {seller.BankAccountNumber}");

            // ✅ Validate bank info
            if (string.IsNullOrWhiteSpace(seller.BankAccountNumber))
            {
                _logger.LogError($"❌ Seller {seller.Id} missing bank account number");
                return;
            }

            string? bankBin = BankBinMapper.GetBinCode(seller.BankCode);
            if (bankBin == null)
            {
                _logger.LogError($"❌ Cannot map bank code '{seller.BankCode}' to BIN");
                return;
            }

            // ✅ Create payout request
            var payoutRequest = new PayoutRequest
            {
                ReferenceId = $"payout_{orderId}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}",
                Amount = payoutAmount,
                ToAccountNumber = seller.BankAccountNumber,
                ToBin = bankBin,
                Description = $"Payout for order {orderId}",
                Category = new List<string> { "FOOD_DELIVERY" }
            };

            _logger.LogInformation($"📤 Sending payout request: {JsonSerializer.Serialize(payoutRequest)}");

            var payoutResult = await _client.Payouts.CreateAsync(payoutRequest);

            _logger.LogInformation($"✅ Payout created successfully!");
            _logger.LogInformation($"   - Payout ID: {payoutResult.Id}");
            _logger.LogInformation($"   - Reference: {payoutResult.ReferenceId}");
            _logger.LogInformation($"   - Approval State: {payoutResult.ApprovalState}");

            // TODO: Lưu payout info vào database
        }
        catch (PayOS.Exceptions.PayOSException payosEx)
        {
            _logger.LogError(payosEx, $"❌ PayOS API error: {payosEx.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ Unexpected payout error for order {orderId}");
        }
    }

    public bool VerifySignature(PayOsWebhookDto callback)
    {
        string checksumKey = _config["PayOS:ChecksumKey"];

        try
        {
            _logger.LogInformation($"🔐 Verifying signature for orderCode {callback.data.orderCode}");

            // PayOS ký chỉ trên 'data' object, không phải toàn bộ webhook
            string computedSignature = _crypto.CreateSignatureFromObject(callback.data, checksumKey);

            _logger.LogInformation($"🔐 Computed: {computedSignature}");
            _logger.LogInformation($"📩 Received: {callback.signature}");

            bool match = computedSignature.Equals(callback.signature, StringComparison.OrdinalIgnoreCase);
            _logger.LogInformation($"✔ Match: {match}");

            return match;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Signature verification exception");
            return false;
        }
    }
}
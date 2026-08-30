using NextDrop.Modules.Orders.Domain.ValueObjects;
using NextDrop.Modules.Payments.Domain.Aggregates;
using NextDrop.Modules.Payments.Domain.ValueObjects;
using NextDrop.SharedKernel.Common;

namespace NextDrop.Modules.Payments.Application.Abstractions;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(PaymentId id, CancellationToken cancellationToken = default);
    Task<Payment?> GetByOrderIdAsync(OrderId orderId, CancellationToken cancellationToken = default);
    Task AddAsync(Payment payment, CancellationToken cancellationToken = default);
}

public interface IRefundRepository
{
    Task<Refund?> GetByIdAsync(RefundId id, CancellationToken cancellationToken = default);
    Task<List<Refund>> GetByPaymentIdAsync(PaymentId paymentId, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalSuccessfulRefundsForPaymentAsync(PaymentId paymentId, CancellationToken cancellationToken = default);
    Task AddAsync(Refund refund, CancellationToken cancellationToken = default);
}

public interface IWebhookEventRepository
{
    Task<WebhookEvent?> GetByProviderEventIdAsync(string provider, string providerEventId, CancellationToken cancellationToken = default);
    Task AddAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken = default);
}

public record PaymentProviderResultDto(
    string ProviderPaymentId,
    string ProviderTransactionId,
    bool IsSuccess,
    string? ErrorMessage);

public interface IPaymentProvider
{
    Task<Result<PaymentProviderResultDto>> CreatePaymentAsync(Payment payment, CancellationToken cancellationToken = default);
    Task<Result<PaymentProviderResultDto>> ConfirmPaymentAsync(Payment payment, CancellationToken cancellationToken = default);
    Task<Result<PaymentProviderResultDto>> RefundPaymentAsync(Payment payment, Refund refund, CancellationToken cancellationToken = default);
    Task<bool> VerifyWebhookSignatureAsync(string provider, string signature, string payload, CancellationToken cancellationToken = default);
}

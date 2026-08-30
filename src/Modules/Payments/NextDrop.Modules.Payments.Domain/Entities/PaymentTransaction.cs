using NextDrop.Modules.Payments.Domain.Enums;
using NextDrop.Modules.Payments.Domain.ValueObjects;
using NextDrop.SharedKernel.Common;

namespace NextDrop.Modules.Payments.Domain.Entities;

public class PaymentTransaction : Entity<PaymentTransactionId>
{
    public PaymentId PaymentId { get; private set; }
    public TransactionType Type { get; private set; }
    public string? ProviderTransactionId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "USD";
    public PaymentTransactionStatus Status { get; private set; }
    public string? ProviderResponseCode { get; private set; }
    public string? ProviderReference { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }

    private PaymentTransaction() { } // EF Core

    internal PaymentTransaction(
        PaymentTransactionId id,
        PaymentId paymentId,
        TransactionType type,
        decimal amount,
        string currency,
        string? providerTransactionId,
        string? providerResponseCode,
        string? providerReference,
        DateTimeOffset now)
        : base(id)
    {
        PaymentId = paymentId;
        Type = type;
        Amount = Math.Round(amount, 2, MidpointRounding.AwayFromZero);
        Currency = currency;
        ProviderTransactionId = providerTransactionId;
        ProviderResponseCode = providerResponseCode;
        ProviderReference = providerReference;
        Status = PaymentTransactionStatus.Success;
        CreatedAtUtc = now;
        CompletedAtUtc = now;
    }
}

using NextDrop.Modules.Orders.Domain.ValueObjects;
using NextDrop.Modules.Payments.Domain.Enums;
using NextDrop.Modules.Payments.Domain.Events;
using NextDrop.Modules.Payments.Domain.ValueObjects;
using NextDrop.SharedKernel.Common;

namespace NextDrop.Modules.Payments.Domain.Aggregates;

public class Refund : AggregateRoot<RefundId>
{
    public PaymentId PaymentId { get; private set; }
    public OrderId OrderId { get; private set; }
    public Guid UserId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "USD";
    public RefundStatus Status { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public string? ProviderRefundId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public uint RowVersion { get; private set; }

    private Refund() { } // EF Core

    private Refund(
        RefundId id,
        PaymentId paymentId,
        OrderId orderId,
        Guid userId,
        decimal amount,
        string currency,
        string reason,
        DateTimeOffset now)
        : base(id)
    {
        PaymentId = paymentId;
        OrderId = orderId;
        UserId = userId;
        Amount = Math.Round(amount, 2, MidpointRounding.AwayFromZero);
        Currency = string.IsNullOrWhiteSpace(currency) ? "USD" : currency.ToUpperInvariant();
        Reason = reason.Trim();
        Status = RefundStatus.Pending;
        CreatedAtUtc = now;
    }

    public static Result<Refund> Create(
        RefundId id,
        Payment payment,
        decimal amount,
        decimal existingTotalRefunds,
        string reason,
        DateTimeOffset now)
    {
        if (payment == null)
            return Result.Failure<Refund>(Error.Validation("Refund.NullPayment", "Payment is required."));

        if (payment.Status != PaymentStatus.Captured && payment.Status != PaymentStatus.PartiallyRefunded)
            return Result.Failure<Refund>(Error.Conflict("Refund.NotCaptured", $"Cannot refund payment in status {payment.Status}. Payment must be Captured."));

        if (amount <= 0)
            return Result.Failure<Refund>(Error.Validation("Refund.InvalidAmount", "Refund amount must be greater than zero."));

        var roundedAmount = Math.Round(amount, 2, MidpointRounding.AwayFromZero);
        var totalAfterRefund = existingTotalRefunds + roundedAmount;

        if (totalAfterRefund > payment.Amount)
            return Result.Failure<Refund>(Error.Conflict("Refund.ExceedsCapturedAmount", $"Total refund amount ({totalAfterRefund}) cannot exceed captured payment amount ({payment.Amount})."));

        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure<Refund>(Error.Validation("Refund.EmptyReason", "Refund reason is required."));

        return new Refund(id, payment.Id, payment.OrderId, payment.UserId, roundedAmount, payment.Currency, reason, now);
    }

    public Result Complete(string providerRefundId, bool isFullRefund, DateTimeOffset now)
    {
        if (Status == RefundStatus.Completed)
            return Result.Failure(Error.Conflict("Refund.AlreadyCompleted", "Refund has already been completed."));

        ProviderRefundId = providerRefundId;
        Status = RefundStatus.Completed;
        CompletedAtUtc = now;

        AddDomainEvent(new PaymentRefundedDomainEvent(PaymentId, Id, Amount, isFullRefund));
        return Result.Success();
    }

    public Result Fail(string reason, DateTimeOffset now)
    {
        if (Status == RefundStatus.Completed)
            return Result.Failure(Error.Conflict("Refund.AlreadyCompleted", "Cannot fail completed refund."));

        Status = RefundStatus.Failed;
        CompletedAtUtc = now;
        return Result.Success();
    }
}

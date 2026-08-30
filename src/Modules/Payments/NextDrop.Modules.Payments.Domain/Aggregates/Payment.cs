using NextDrop.Modules.Orders.Domain.ValueObjects;
using NextDrop.Modules.Payments.Domain.Entities;
using NextDrop.Modules.Payments.Domain.Enums;
using NextDrop.Modules.Payments.Domain.Events;
using NextDrop.Modules.Payments.Domain.ValueObjects;
using NextDrop.SharedKernel.Common;

namespace NextDrop.Modules.Payments.Domain.Aggregates;

public class Payment : AggregateRoot<PaymentId>
{
    private readonly List<PaymentTransaction> _transactions = new();

    public OrderId OrderId { get; private set; }
    public Guid UserId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "USD";
    public PaymentStatus Status { get; private set; }
    public PaymentProvider Provider { get; private set; }
    public string? ProviderPaymentId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset? CapturedAtUtc { get; private set; }
    public DateTimeOffset? FailedAtUtc { get; private set; }
    public DateTimeOffset? CancelledAtUtc { get; private set; }
    public uint RowVersion { get; private set; }

    public IReadOnlyCollection<PaymentTransaction> Transactions => _transactions.AsReadOnly();

    private Payment() { } // EF Core

    private Payment(
        PaymentId id,
        OrderId orderId,
        Guid userId,
        decimal amount,
        string currency,
        PaymentProvider provider,
        DateTimeOffset now)
        : base(id)
    {
        OrderId = orderId;
        UserId = userId;
        Amount = Math.Round(amount, 2, MidpointRounding.AwayFromZero);
        Currency = string.IsNullOrWhiteSpace(currency) ? "USD" : currency.ToUpperInvariant();
        Provider = provider;
        Status = PaymentStatus.Pending;
        CreatedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public static Result<Payment> Create(
        PaymentId id,
        OrderId orderId,
        Guid userId,
        decimal amount,
        string currency,
        PaymentProvider provider,
        DateTimeOffset now)
    {
        if (orderId == OrderId.Empty)
            return Result.Failure<Payment>(Error.Validation("Payment.EmptyOrder", "OrderId is required."));

        if (userId == Guid.Empty)
            return Result.Failure<Payment>(Error.Validation("Payment.EmptyUser", "UserId is required."));

        if (amount <= 0)
            return Result.Failure<Payment>(Error.Validation("Payment.InvalidAmount", "Payment amount must be greater than zero."));

        var payment = new Payment(id, orderId, userId, amount, currency, provider, now);
        payment.AddDomainEvent(new PaymentCreatedDomainEvent(id, orderId, payment.Amount));
        return payment;
    }

    public Result StartProcessing(DateTimeOffset now)
    {
        if (Status != PaymentStatus.Pending)
            return Result.Failure(Error.Conflict("Payment.InvalidTransition", $"Cannot process payment in status {Status}."));

        Status = PaymentStatus.Processing;
        UpdatedAtUtc = now;
        return Result.Success();
    }

    public Result Authorize(string providerPaymentId, DateTimeOffset now)
    {
        if (Status != PaymentStatus.Pending && Status != PaymentStatus.Processing)
            return Result.Failure(Error.Conflict("Payment.InvalidTransition", $"Cannot authorize payment in status {Status}."));

        ProviderPaymentId = providerPaymentId;
        Status = PaymentStatus.Authorized;
        UpdatedAtUtc = now;

        _transactions.Add(new PaymentTransaction(
            PaymentTransactionId.New(),
            Id,
            TransactionType.Authorization,
            Amount,
            Currency,
            providerPaymentId,
            "200",
            "Authorized",
            now));

        AddDomainEvent(new PaymentAuthorizedDomainEvent(Id, OrderId, providerPaymentId));
        return Result.Success();
    }

    public Result Capture(string providerPaymentId, DateTimeOffset now)
    {
        if (Status == PaymentStatus.Captured || Status == PaymentStatus.Refunded)
            return Result.Failure(Error.Conflict("Payment.AlreadyCaptured", "Payment has already been captured."));

        if (Status == PaymentStatus.Failed || Status == PaymentStatus.Cancelled)
            return Result.Failure(Error.Conflict("Payment.TerminalState", $"Cannot capture payment in status {Status}."));

        ProviderPaymentId = providerPaymentId;
        Status = PaymentStatus.Captured;
        CapturedAtUtc = now;
        UpdatedAtUtc = now;

        _transactions.Add(new PaymentTransaction(
            PaymentTransactionId.New(),
            Id,
            TransactionType.Capture,
            Amount,
            Currency,
            providerPaymentId,
            "200",
            "Captured",
            now));

        AddDomainEvent(new PaymentCapturedDomainEvent(Id, OrderId, Amount));
        return Result.Success();
    }

    public Result Fail(string reason, DateTimeOffset now)
    {
        if (Status == PaymentStatus.Captured || Status == PaymentStatus.Refunded)
            return Result.Failure(Error.Conflict("Payment.CannotFailCaptured", "Cannot fail a captured payment."));

        Status = PaymentStatus.Failed;
        FailedAtUtc = now;
        UpdatedAtUtc = now;

        AddDomainEvent(new PaymentFailedDomainEvent(Id, OrderId, reason));
        return Result.Success();
    }

    public Result Cancel(DateTimeOffset now)
    {
        if (Status == PaymentStatus.Captured || Status == PaymentStatus.Refunded)
            return Result.Failure(Error.Conflict("Payment.CannotCancelCaptured", "Cannot cancel a captured payment."));

        Status = PaymentStatus.Cancelled;
        CancelledAtUtc = now;
        UpdatedAtUtc = now;

        AddDomainEvent(new PaymentCancelledDomainEvent(Id, OrderId));
        return Result.Success();
    }

    public Result MarkPartiallyRefunded(DateTimeOffset now)
    {
        if (Status != PaymentStatus.Captured && Status != PaymentStatus.PartiallyRefunded)
            return Result.Failure(Error.Conflict("Payment.CannotRefund", $"Cannot refund payment in status {Status}."));

        Status = PaymentStatus.PartiallyRefunded;
        UpdatedAtUtc = now;
        return Result.Success();
    }

    public Result MarkRefunded(DateTimeOffset now)
    {
        if (Status != PaymentStatus.Captured && Status != PaymentStatus.PartiallyRefunded)
            return Result.Failure(Error.Conflict("Payment.CannotRefund", $"Cannot refund payment in status {Status}."));

        Status = PaymentStatus.Refunded;
        UpdatedAtUtc = now;
        return Result.Success();
    }
}

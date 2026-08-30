using NextDrop.SharedKernel.Common;

namespace NextDrop.Modules.Payments.Domain.ValueObjects;

public readonly record struct PaymentId(Guid Value)
{
    public static PaymentId New() => new(Guid.NewGuid());
    public static PaymentId Empty => new(Guid.Empty);
}

public readonly record struct PaymentTransactionId(Guid Value)
{
    public static PaymentTransactionId New() => new(Guid.NewGuid());
    public static PaymentTransactionId Empty => new(Guid.Empty);
}

public readonly record struct RefundId(Guid Value)
{
    public static RefundId New() => new(Guid.NewGuid());
    public static RefundId Empty => new(Guid.Empty);
}

public readonly record struct WebhookEventId(Guid Value)
{
    public static WebhookEventId New() => new(Guid.NewGuid());
    public static WebhookEventId Empty => new(Guid.Empty);
}

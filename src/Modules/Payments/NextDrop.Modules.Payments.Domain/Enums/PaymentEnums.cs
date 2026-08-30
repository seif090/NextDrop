namespace NextDrop.Modules.Payments.Domain.Enums;

public enum PaymentStatus
{
    Pending = 1,
    Processing = 2,
    Authorized = 3,
    Captured = 4,
    PartiallyRefunded = 5,
    Refunded = 6,
    Failed = 7,
    Cancelled = 8
}

public enum PaymentProvider
{
    FakeProvider = 1,
    Stripe = 2,
    PayPal = 3,
    PayMob = 4
}

public enum TransactionType
{
    Authorization = 1,
    Capture = 2,
    Refund = 3,
    Void = 4
}

public enum PaymentTransactionStatus
{
    Pending = 1,
    Success = 2,
    Failed = 3
}

public enum RefundStatus
{
    Pending = 1,
    Completed = 2,
    Failed = 3
}

public enum WebhookProcessingStatus
{
    Pending = 1,
    Processed = 2,
    Failed = 3,
    Ignored = 4
}

namespace NextDrop.Modules.Notifications.Domain.Enums;

public enum NotificationType
{
    OrderPlaced = 1,
    OrderConfirmed = 2,
    OrderPreparing = 3,
    OrderReady = 4,
    RiderAssigned = 5,
    RiderArriving = 6,
    OrderPickedUp = 7,
    OrderOutForDelivery = 8,
    OrderDelivered = 9,
    OrderCancelled = 10,
    PaymentSucceeded = 11,
    PaymentFailed = 12,
    RefundCompleted = 13,
    Marketing = 14,
    System = 15
}

public enum NotificationChannel
{
    InApp = 1,
    Email = 2,
    Push = 3,
    Sms = 4
}

public enum NotificationPriority
{
    Low = 1,
    Normal = 2,
    High = 3,
    Urgent = 4
}

public enum NotificationStatus
{
    Unread = 1,
    Read = 2,
    Archived = 3
}

public enum DeliveryStatus
{
    Pending = 1,
    Processing = 2,
    Delivered = 3,
    Failed = 4,
    DeadLettered = 5
}

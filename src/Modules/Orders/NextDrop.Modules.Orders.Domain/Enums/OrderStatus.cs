namespace NextDrop.Modules.Orders.Domain.Enums;

public enum OrderStatus
{
    PendingPayment = 0,
    Pending = 1,
    Paid = 2,
    Confirmed = 3,
    Preparing = 4,
    ReadyForDelivery = 5,
    PickedUp = 6,
    OutForDelivery = 7,
    Delivered = 8,
    Cancelled = 9,
    Failed = 10,
    Refunded = 11
}

namespace NextDrop.Modules.Orders.Domain.Enums;

public enum OrderStatus
{
    Pending = 1,
    Confirmed = 2,
    Preparing = 3,
    ReadyForDelivery = 4,
    OutForDelivery = 5,
    Delivered = 6,
    Cancelled = 7
}

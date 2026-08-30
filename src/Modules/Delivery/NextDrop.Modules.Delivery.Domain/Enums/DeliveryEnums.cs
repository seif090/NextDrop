namespace NextDrop.Modules.Delivery.Domain.Enums;

public enum RiderStatus
{
    Pending = 1,
    Active = 2,
    Suspended = 3,
    Blocked = 4,
    Archived = 5
}

public enum RiderAvailabilityStatus
{
    Offline = 1,
    Available = 2,
    Busy = 3
}

public enum VehicleType
{
    Motorcycle = 1,
    Car = 2,
    Bicycle = 3,
    Scooter = 4,
    Other = 5
}

public enum DeliveryStatus
{
    Pending = 1,
    SearchingForRider = 2,
    Assigned = 3,
    RiderArrivedAtRestaurant = 4,
    PickedUp = 5,
    OutForDelivery = 6,
    Delivered = 7,
    Failed = 8,
    Cancelled = 9
}

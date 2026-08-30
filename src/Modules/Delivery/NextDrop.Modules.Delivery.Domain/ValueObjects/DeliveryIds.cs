using NextDrop.SharedKernel.Common;

namespace NextDrop.Modules.Delivery.Domain.ValueObjects;

public readonly record struct RiderId(Guid Value)
{
    public static RiderId New() => new(Guid.NewGuid());
    public static RiderId Empty => new(Guid.Empty);
}

public readonly record struct DeliveryId(Guid Value)
{
    public static DeliveryId New() => new(Guid.NewGuid());
    public static DeliveryId Empty => new(Guid.Empty);
}

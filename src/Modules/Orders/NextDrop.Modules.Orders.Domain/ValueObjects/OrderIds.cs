using NextDrop.SharedKernel.Common;

namespace NextDrop.Modules.Orders.Domain.ValueObjects;

public readonly record struct CartId(Guid Value)
{
    public static CartId New() => new(Guid.NewGuid());
    public static CartId Empty => new(Guid.Empty);
}

public readonly record struct CartItemId(Guid Value)
{
    public static CartItemId New() => new(Guid.NewGuid());
    public static CartItemId Empty => new(Guid.Empty);
}

public readonly record struct OrderId(Guid Value)
{
    public static OrderId New() => new(Guid.NewGuid());
    public static OrderId Empty => new(Guid.Empty);
}

public readonly record struct OrderItemId(Guid Value)
{
    public static OrderItemId New() => new(Guid.NewGuid());
    public static OrderItemId Empty => new(Guid.Empty);
}

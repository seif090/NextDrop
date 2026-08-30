namespace NextDrop.Modules.Restaurants.Domain.ValueObjects;

public readonly record struct RestaurantId(Guid Value)
{
    public static RestaurantId New() => new(Guid.NewGuid());
    public static RestaurantId Empty => new(Guid.Empty);
    public static RestaurantId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}

public readonly record struct RestaurantBranchId(Guid Value)
{
    public static RestaurantBranchId New() => new(Guid.NewGuid());
    public static RestaurantBranchId Empty => new(Guid.Empty);
    public static RestaurantBranchId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}

public readonly record struct RestaurantStaffMembershipId(Guid Value)
{
    public static RestaurantStaffMembershipId New() => new(Guid.NewGuid());
    public static RestaurantStaffMembershipId Empty => new(Guid.Empty);
    public static RestaurantStaffMembershipId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}

public readonly record struct RestaurantDeliveryZoneId(Guid Value)
{
    public static RestaurantDeliveryZoneId New() => new(Guid.NewGuid());
    public static RestaurantDeliveryZoneId Empty => new(Guid.Empty);
    public static RestaurantDeliveryZoneId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}

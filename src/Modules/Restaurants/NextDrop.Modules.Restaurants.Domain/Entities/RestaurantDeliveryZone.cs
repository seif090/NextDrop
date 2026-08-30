using NextDrop.SharedKernel.Common;
using NextDrop.Modules.Restaurants.Domain.ValueObjects;

namespace NextDrop.Modules.Restaurants.Domain.Entities;

public class RestaurantDeliveryZone : Entity<RestaurantDeliveryZoneId>
{
    public RestaurantBranchId BranchId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal DeliveryFee { get; private set; }
    public decimal MinimumOrderAmount { get; private set; }
    public int EstimatedDeliveryMinutes { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private RestaurantDeliveryZone() { } // EF Core

    public RestaurantDeliveryZone(
        RestaurantDeliveryZoneId id,
        RestaurantBranchId branchId,
        string name,
        decimal deliveryFee,
        decimal minimumOrderAmount,
        int estimatedDeliveryMinutes,
        DateTimeOffset now)
        : base(id)
    {
        BranchId = branchId;
        Name = name;
        DeliveryFee = deliveryFee;
        MinimumOrderAmount = minimumOrderAmount;
        EstimatedDeliveryMinutes = estimatedDeliveryMinutes;
        IsActive = true;
        CreatedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public void Update(
        string name,
        decimal deliveryFee,
        decimal minimumOrderAmount,
        int estimatedDeliveryMinutes,
        DateTimeOffset now)
    {
        Name = name;
        DeliveryFee = deliveryFee;
        MinimumOrderAmount = minimumOrderAmount;
        EstimatedDeliveryMinutes = estimatedDeliveryMinutes;
        UpdatedAtUtc = now;
    }

    public void Deactivate(DateTimeOffset now)
    {
        IsActive = false;
        UpdatedAtUtc = now;
    }
}

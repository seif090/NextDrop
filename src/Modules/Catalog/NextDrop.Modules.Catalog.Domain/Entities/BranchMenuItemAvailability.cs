using NextDrop.Modules.Catalog.Domain.ValueObjects;
using NextDrop.Modules.Restaurants.Domain.ValueObjects;
using NextDrop.SharedKernel.Common;

namespace NextDrop.Modules.Catalog.Domain.Entities;

public class BranchMenuItemAvailability : Entity<BranchMenuItemAvailabilityId>
{
    public MenuItemId MenuItemId { get; private set; }
    public RestaurantBranchId RestaurantBranchId { get; private set; }
    public RestaurantId RestaurantId { get; private set; }
    public bool IsAvailable { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private BranchMenuItemAvailability() { } // EF Core

    public BranchMenuItemAvailability(
        BranchMenuItemAvailabilityId id,
        MenuItemId menuItemId,
        RestaurantBranchId restaurantBranchId,
        RestaurantId restaurantId,
        bool isAvailable,
        DateTimeOffset now)
        : base(id)
    {
        MenuItemId = menuItemId;
        RestaurantBranchId = restaurantBranchId;
        RestaurantId = restaurantId;
        IsAvailable = isAvailable;
        UpdatedAtUtc = now;
    }

    public void SetAvailability(bool isAvailable, DateTimeOffset now)
    {
        IsAvailable = isAvailable;
        UpdatedAtUtc = now;
    }
}

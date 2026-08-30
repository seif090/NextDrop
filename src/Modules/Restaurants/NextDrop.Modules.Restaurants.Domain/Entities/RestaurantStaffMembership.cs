using NextDrop.SharedKernel.Common;
using NextDrop.Modules.Restaurants.Domain.Enums;
using NextDrop.Modules.Restaurants.Domain.ValueObjects;

namespace NextDrop.Modules.Restaurants.Domain.Entities;

public class RestaurantStaffMembership : Entity<RestaurantStaffMembershipId>
{
    public RestaurantId RestaurantId { get; private set; }
    public Guid UserId { get; private set; }
    public RestaurantStaffRole Role { get; private set; }
    public RestaurantStaffStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    private RestaurantStaffMembership() { } // EF Core

    public RestaurantStaffMembership(
        RestaurantStaffMembershipId id,
        RestaurantId restaurantId,
        Guid userId,
        RestaurantStaffRole role,
        DateTimeOffset now)
        : base(id)
    {
        RestaurantId = restaurantId;
        UserId = userId;
        Role = role;
        Status = RestaurantStaffStatus.Active;
        CreatedAtUtc = now;
    }

    public void UpdateRole(RestaurantStaffRole role)
    {
        Role = role;
    }

    public void Deactivate()
    {
        Status = RestaurantStaffStatus.Inactive;
    }
}

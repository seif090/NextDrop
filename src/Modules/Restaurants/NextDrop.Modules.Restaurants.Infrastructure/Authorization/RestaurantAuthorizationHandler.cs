using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using NextDrop.Modules.Restaurants.Application.Abstractions;
using NextDrop.Modules.Restaurants.Domain.Enums;
using NextDrop.Modules.Restaurants.Domain.ValueObjects;

namespace NextDrop.Modules.Restaurants.Infrastructure.Authorization;

public class ManageRestaurantRequirement : IAuthorizationRequirement { }

public class RestaurantAuthorizationHandler : AuthorizationHandler<ManageRestaurantRequirement, Guid>
{
    private readonly IRestaurantRepository _restaurantRepository;

    public RestaurantAuthorizationHandler(IRestaurantRepository restaurantRepository)
    {
        _restaurantRepository = restaurantRepository;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ManageRestaurantRequirement requirement,
        Guid restaurantId)
    {
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? context.User.FindFirst("sub")?.Value;

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return;
        }

        var restaurant = await _restaurantRepository.GetByIdAsync(RestaurantId.From(restaurantId));
        if (restaurant == null)
        {
            return;
        }

        if (restaurant.UserHasRole(userId, RestaurantStaffRole.Owner, RestaurantStaffRole.Manager))
        {
            context.Succeed(requirement);
        }
    }
}

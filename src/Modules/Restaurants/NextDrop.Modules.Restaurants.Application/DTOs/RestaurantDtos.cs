namespace NextDrop.Modules.Restaurants.Application.DTOs;

public record RestaurantOperatingHoursDto(
    string DayOfWeek,
    string OpenTime,
    string CloseTime,
    bool IsClosed);

public record RestaurantDeliveryZoneDto(
    Guid Id,
    Guid BranchId,
    string Name,
    decimal DeliveryFee,
    decimal MinimumOrderAmount,
    int EstimatedDeliveryMinutes,
    bool IsActive);

public record RestaurantBranchDto(
    Guid Id,
    Guid RestaurantId,
    string Name,
    string PhoneNumber,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string District,
    decimal Latitude,
    decimal Longitude,
    string Timezone,
    string Status,
    IReadOnlyList<RestaurantOperatingHoursDto> OperatingHours,
    IReadOnlyList<RestaurantDeliveryZoneDto> DeliveryZones);

public record RestaurantStaffMembershipDto(
    Guid Id,
    Guid RestaurantId,
    Guid UserId,
    string Role,
    string Status,
    DateTimeOffset CreatedAtUtc);

public record RestaurantDto(
    Guid Id,
    Guid OwnerUserId,
    string Name,
    string Description,
    string PhoneNumber,
    string Email,
    string Status,
    IReadOnlyList<RestaurantBranchDto> Branches,
    DateTimeOffset CreatedAtUtc);

public record RestaurantSummaryDto(
    Guid Id,
    string Name,
    string Status,
    string? PrimaryCity,
    int ActiveBranchesCount);

public record PagedRestaurantResponse(
    IReadOnlyList<RestaurantSummaryDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

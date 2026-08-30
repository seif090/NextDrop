namespace NextDrop.Modules.Discovery.Application.DTOs;

public record PublicBranchDto(
    Guid Id,
    Guid RestaurantId,
    string BranchName,
    string AddressLine,
    string City,
    string District,
    string Timezone,
    string Status,
    bool IsOpenNow,
    decimal MinimumOrderAmount,
    decimal EstimatedDeliveryFee,
    int EstimatedDeliveryTimeMinutes);

public record PublicRestaurantDto(
    Guid Id,
    string Name,
    string Description,
    string PhoneNumber,
    string Email,
    string Status,
    List<PublicBranchDto> Branches);

public record PublicMenuItemDto(
    Guid Id,
    Guid RestaurantId,
    Guid CategoryId,
    string CategoryName,
    string Name,
    string Description,
    decimal BasePrice,
    string? ImageUrl,
    bool IsAvailable,
    bool IsPublished);

public record PagedDiscoveryResultDto<T>(
    List<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasNextPage,
    bool HasPreviousPage);

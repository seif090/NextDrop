using NextDrop.Modules.Discovery.Domain.Enums;
using NextDrop.SharedKernel.Common;

namespace NextDrop.Modules.Discovery.Domain.ValueObjects;

public record RestaurantDiscoveryCriteria
{
    public string? SearchTerm { get; init; }
    public string? City { get; init; }
    public string? District { get; init; }
    public bool OpenNow { get; init; }
    public decimal? MinOrderAmount { get; init; }
    public decimal? MaxDeliveryFee { get; init; }
    public int? MinEstDeliveryTimeMinutes { get; init; }
    public int? MaxEstDeliveryTimeMinutes { get; init; }
    public DiscoverySort Sort { get; init; } = DiscoverySort.Relevance;
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;

    public static Result<RestaurantDiscoveryCriteria> Create(
        string? searchTerm,
        string? city,
        string? district,
        bool openNow,
        decimal? minOrderAmount,
        decimal? maxDeliveryFee,
        int? minEstDeliveryTimeMinutes,
        int? maxEstDeliveryTimeMinutes,
        DiscoverySort sort = DiscoverySort.Relevance,
        int page = 1,
        int pageSize = 20)
    {
        if (!string.IsNullOrWhiteSpace(searchTerm) && searchTerm.Length > 100)
            return Result.Failure<RestaurantDiscoveryCriteria>(Error.Validation("Discovery.SearchTermTooLong", "Search term must not exceed 100 characters."));

        if (page < 1)
            return Result.Failure<RestaurantDiscoveryCriteria>(Error.Validation("Discovery.InvalidPage", "Page must be at least 1."));

        if (pageSize < 1 || pageSize > 100)
            return Result.Failure<RestaurantDiscoveryCriteria>(Error.Validation("Discovery.InvalidPageSize", "PageSize must be between 1 and 100."));

        return new RestaurantDiscoveryCriteria
        {
            SearchTerm = searchTerm?.Trim(),
            City = city?.Trim(),
            District = district?.Trim(),
            OpenNow = openNow,
            MinOrderAmount = minOrderAmount,
            MaxDeliveryFee = maxDeliveryFee,
            MinEstDeliveryTimeMinutes = minEstDeliveryTimeMinutes,
            MaxEstDeliveryTimeMinutes = maxEstDeliveryTimeMinutes,
            Sort = sort,
            Page = page,
            PageSize = pageSize
        };
    }
}

public record MenuItemDiscoveryCriteria
{
    public Guid? RestaurantId { get; init; }
    public Guid? BranchId { get; init; }
    public Guid? CategoryId { get; init; }
    public string? SearchTerm { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public bool AvailableOnly { get; init; } = true;
    public bool PublishedOnly { get; init; } = true;
    public MenuItemSort Sort { get; init; } = MenuItemSort.Relevance;
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;

    public static Result<MenuItemDiscoveryCriteria> Create(
        Guid? restaurantId,
        Guid? branchId,
        Guid? categoryId,
        string? searchTerm,
        decimal? minPrice,
        decimal? maxPrice,
        bool availableOnly = true,
        bool publishedOnly = true,
        MenuItemSort sort = MenuItemSort.Relevance,
        int page = 1,
        int pageSize = 20)
    {
        if (!string.IsNullOrWhiteSpace(searchTerm) && searchTerm.Length > 100)
            return Result.Failure<MenuItemDiscoveryCriteria>(Error.Validation("Discovery.SearchTermTooLong", "Search term must not exceed 100 characters."));

        if (page < 1)
            return Result.Failure<MenuItemDiscoveryCriteria>(Error.Validation("Discovery.InvalidPage", "Page must be at least 1."));

        if (pageSize < 1 || pageSize > 100)
            return Result.Failure<MenuItemDiscoveryCriteria>(Error.Validation("Discovery.InvalidPageSize", "PageSize must be between 1 and 100."));

        return new MenuItemDiscoveryCriteria
        {
            RestaurantId = restaurantId,
            BranchId = branchId,
            CategoryId = categoryId,
            SearchTerm = searchTerm?.Trim(),
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            AvailableOnly = availableOnly,
            PublishedOnly = publishedOnly,
            Sort = sort,
            Page = page,
            PageSize = pageSize
        };
    }
}

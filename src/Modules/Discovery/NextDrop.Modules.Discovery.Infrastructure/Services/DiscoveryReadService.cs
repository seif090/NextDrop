using Microsoft.EntityFrameworkCore;
using NextDrop.Infrastructure.Persistence;
using NextDrop.Modules.Catalog.Domain.ValueObjects;
using NextDrop.Modules.Discovery.Application.Abstractions;
using NextDrop.Modules.Discovery.Application.DTOs;
using NextDrop.Modules.Discovery.Domain.Enums;
using NextDrop.Modules.Discovery.Domain.ValueObjects;
using NextDrop.Modules.Restaurants.Domain.Entities;
using NextDrop.Modules.Restaurants.Domain.Enums;
using NextDrop.Modules.Restaurants.Domain.ValueObjects;
using NextDrop.SharedKernel.Abstractions;

namespace NextDrop.Modules.Discovery.Infrastructure.Services;

public class DiscoveryReadService : IDiscoveryReadService
{
    private readonly NextDropDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DiscoveryReadService(NextDropDbContext dbContext, IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<PagedDiscoveryResultDto<PublicRestaurantDto>> GetPublicRestaurantsAsync(
        RestaurantDiscoveryCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        var nowUtc = _dateTimeProvider.UtcNow;

        var query = _dbContext.Restaurants
            .AsNoTracking()
            .Include(r => r.Branches)
            .ThenInclude(b => b.OperatingHours)
            .Include(r => r.Branches)
            .ThenInclude(b => b.DeliveryZones)
            .Where(r => r.Status == RestaurantStatus.Active)
            .AsQueryable();

        // City / District filter
        if (!string.IsNullOrWhiteSpace(criteria.City))
        {
            var cityLower = criteria.City.Trim().ToLower();
            query = query.Where(r => r.Branches.Any(b => b.Status == BranchStatus.Active && b.City.ToLower() == cityLower));
        }

        if (!string.IsNullOrWhiteSpace(criteria.District))
        {
            var districtLower = criteria.District.Trim().ToLower();
            query = query.Where(r => r.Branches.Any(b => b.Status == BranchStatus.Active && b.District.ToLower() == districtLower));
        }

        // Search term filter
        if (!string.IsNullOrWhiteSpace(criteria.SearchTerm))
        {
            var term = criteria.SearchTerm.Trim().ToLower();
            query = query.Where(r => r.Name.ToLower().Contains(term) || r.Description.ToLower().Contains(term));
        }

        var restaurants = await query.ToListAsync(cancellationToken);

        // Filter branches per restaurant to active branches
        var projectedList = new List<(PublicRestaurantDto Dto, int RelevanceScore, decimal MinFee, decimal MinOrder, int MinDeliveryTime)>();

        foreach (var r in restaurants)
        {
            var activeBranches = r.Branches.Where(b => b.Status == BranchStatus.Active).ToList();
            if (!activeBranches.Any())
                continue;

            var branchDtos = new List<PublicBranchDto>();
            bool hasOpenBranch = false;

            foreach (var branch in activeBranches)
            {
                var isOpen = IsBranchOpenNow(branch, nowUtc);
                if (isOpen) hasOpenBranch = true;

                var deliveryZone = branch.DeliveryZones.FirstOrDefault();
                var minOrder = deliveryZone?.MinimumOrderAmount ?? 0m;
                var delFee = deliveryZone?.DeliveryFee ?? 0m;
                var delTime = deliveryZone?.EstimatedDeliveryMinutes ?? 30;

                branchDtos.Add(new PublicBranchDto(
                    branch.Id.Value,
                    r.Id.Value,
                    branch.Name,
                    string.IsNullOrWhiteSpace(branch.AddressLine2) ? branch.AddressLine1 : $"{branch.AddressLine1}, {branch.AddressLine2}",
                    branch.City,
                    branch.District,
                    branch.Timezone,
                    branch.Status.ToString(),
                    isOpen,
                    minOrder,
                    delFee,
                    delTime));
            }

            if (criteria.OpenNow && !hasOpenBranch)
                continue;

            var minDeliveryFee = branchDtos.Min(b => b.EstimatedDeliveryFee);
            var minOrderAmount = branchDtos.Min(b => b.MinimumOrderAmount);
            var minDeliveryTime = branchDtos.Min(b => b.EstimatedDeliveryTimeMinutes);

            if (criteria.MinOrderAmount.HasValue && minOrderAmount < criteria.MinOrderAmount.Value)
                continue;

            if (criteria.MaxDeliveryFee.HasValue && minDeliveryFee > criteria.MaxDeliveryFee.Value)
                continue;

            if (criteria.MinEstDeliveryTimeMinutes.HasValue && minDeliveryTime < criteria.MinEstDeliveryTimeMinutes.Value)
                continue;

            if (criteria.MaxEstDeliveryTimeMinutes.HasValue && minDeliveryTime > criteria.MaxEstDeliveryTimeMinutes.Value)
                continue;

            int relevanceScore = ComputeRestaurantRelevanceScore(r.Name, r.Description, criteria.SearchTerm);

            var dto = new PublicRestaurantDto(
                r.Id.Value,
                r.Name,
                r.Description,
                r.PhoneNumber,
                r.Email,
                r.Status.ToString(),
                branchDtos);

            projectedList.Add((dto, relevanceScore, minDeliveryFee, minOrderAmount, minDeliveryTime));
        }

        // Sorting
        IEnumerable<(PublicRestaurantDto Dto, int RelevanceScore, decimal MinFee, decimal MinOrder, int MinDeliveryTime)> sorted = criteria.Sort switch
        {
            DiscoverySort.NameAscending => projectedList.OrderBy(x => x.Dto.Name),
            DiscoverySort.NameDescending => projectedList.OrderByDescending(x => x.Dto.Name),
            DiscoverySort.DeliveryFeeAscending => projectedList.OrderBy(x => x.MinFee).ThenBy(x => x.Dto.Name),
            DiscoverySort.DeliveryFeeDescending => projectedList.OrderByDescending(x => x.MinFee).ThenBy(x => x.Dto.Name),
            DiscoverySort.MinimumOrderAscending => projectedList.OrderBy(x => x.MinOrder).ThenBy(x => x.Dto.Name),
            DiscoverySort.MinimumOrderDescending => projectedList.OrderByDescending(x => x.MinOrder).ThenBy(x => x.Dto.Name),
            DiscoverySort.FastestDelivery => projectedList.OrderBy(x => x.MinDeliveryTime).ThenBy(x => x.Dto.Name),
            _ => projectedList.OrderByDescending(x => x.RelevanceScore).ThenBy(x => x.Dto.Name)
        };

        var finalOrderedList = sorted.Select(x => x.Dto).ToList();
        var totalCount = finalOrderedList.Count;
        var totalPages = (int)Math.Ceiling((double)totalCount / criteria.PageSize);
        var pagedItems = finalOrderedList
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .ToList();

        return new PagedDiscoveryResultDto<PublicRestaurantDto>(
            pagedItems,
            criteria.Page,
            criteria.PageSize,
            totalCount,
            totalPages,
            criteria.Page > 1 && criteria.Page <= totalPages,
            criteria.Page < totalPages);
    }

    public async Task<PublicRestaurantDto?> GetPublicRestaurantByIdAsync(Guid restaurantId, CancellationToken cancellationToken = default)
    {
        var nowUtc = _dateTimeProvider.UtcNow;

        var restaurant = await _dbContext.Restaurants
            .AsNoTracking()
            .Include(r => r.Branches)
            .ThenInclude(b => b.OperatingHours)
            .Include(r => r.Branches)
            .ThenInclude(b => b.DeliveryZones)
            .FirstOrDefaultAsync(r => r.Id == RestaurantId.From(restaurantId) && r.Status == RestaurantStatus.Active, cancellationToken);

        if (restaurant == null)
            return null;

        var activeBranches = restaurant.Branches.Where(b => b.Status == BranchStatus.Active).Select(b =>
        {
            var isOpen = IsBranchOpenNow(b, nowUtc);
            var zone = b.DeliveryZones.FirstOrDefault();
            return new PublicBranchDto(
                b.Id.Value,
                restaurant.Id.Value,
                b.Name,
                string.IsNullOrWhiteSpace(b.AddressLine2) ? b.AddressLine1 : $"{b.AddressLine1}, {b.AddressLine2}",
                b.City,
                b.District,
                b.Timezone,
                b.Status.ToString(),
                isOpen,
                zone?.MinimumOrderAmount ?? 0m,
                zone?.DeliveryFee ?? 0m,
                zone?.EstimatedDeliveryMinutes ?? 30);
        }).ToList();

        return new PublicRestaurantDto(
            restaurant.Id.Value,
            restaurant.Name,
            restaurant.Description,
            restaurant.PhoneNumber,
            restaurant.Email,
            restaurant.Status.ToString(),
            activeBranches);
    }

    public async Task<List<PublicBranchDto>> GetPublicRestaurantBranchesAsync(Guid restaurantId, CancellationToken cancellationToken = default)
    {
        var nowUtc = _dateTimeProvider.UtcNow;

        var restaurant = await _dbContext.Restaurants
            .AsNoTracking()
            .Include(r => r.Branches)
            .ThenInclude(b => b.OperatingHours)
            .Include(r => r.Branches)
            .ThenInclude(b => b.DeliveryZones)
            .FirstOrDefaultAsync(r => r.Id == RestaurantId.From(restaurantId) && r.Status == RestaurantStatus.Active, cancellationToken);

        if (restaurant == null)
            return new List<PublicBranchDto>();

        return restaurant.Branches
            .Where(b => b.Status == BranchStatus.Active)
            .Select(b =>
            {
                var isOpen = IsBranchOpenNow(b, nowUtc);
                var zone = b.DeliveryZones.FirstOrDefault();
                return new PublicBranchDto(
                    b.Id.Value,
                    restaurant.Id.Value,
                    b.Name,
                    string.IsNullOrWhiteSpace(b.AddressLine2) ? b.AddressLine1 : $"{b.AddressLine1}, {b.AddressLine2}",
                    b.City,
                    b.District,
                    b.Timezone,
                    b.Status.ToString(),
                    isOpen,
                    zone?.MinimumOrderAmount ?? 0m,
                    zone?.DeliveryFee ?? 0m,
                    zone?.EstimatedDeliveryMinutes ?? 30);
            })
            .ToList();
    }

    public async Task<PagedDiscoveryResultDto<PublicMenuItemDto>> GetPublicMenuItemsAsync(
        MenuItemDiscoveryCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.MenuItems
            .AsNoTracking()
            .Where(m => m.IsActive)
            .AsQueryable();

        if (criteria.RestaurantId.HasValue)
        {
            var rId = RestaurantId.From(criteria.RestaurantId.Value);
            query = query.Where(m => m.RestaurantId == rId);
        }

        if (criteria.CategoryId.HasValue)
        {
            var catId = new CategoryId(criteria.CategoryId.Value);
            query = query.Where(m => m.CategoryId == catId);
        }

        if (!string.IsNullOrWhiteSpace(criteria.SearchTerm))
        {
            var term = criteria.SearchTerm.Trim().ToLower();
            query = query.Where(m => (m.Name != null && m.Name.ToLower().Contains(term)) || (m.Description != null && m.Description.ToLower().Contains(term)));
        }

        if (criteria.MinPrice.HasValue)
        {
            query = query.Where(m => m.BasePrice >= criteria.MinPrice.Value);
        }

        if (criteria.MaxPrice.HasValue)
        {
            query = query.Where(m => m.BasePrice <= criteria.MaxPrice.Value);
        }

        var items = await query.ToListAsync(cancellationToken);

        // Map Category Names
        var categoryIds = items.Select(i => i.CategoryId).Distinct().ToList();

        var categories = await _dbContext.Set<NextDrop.Modules.Catalog.Domain.Entities.Category>()
            .AsNoTracking()
            .Where(c => categoryIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);

        // Branch availability filtering if branchId supplied
        HashSet<Guid>? availableMenuItemIds = null;
        if (criteria.BranchId.HasValue && criteria.AvailableOnly)
        {
            var branchAvailabilities = await _dbContext.BranchMenuItemAvailabilities
                .AsNoTracking()
                .Where(b => b.RestaurantBranchId == RestaurantBranchId.From(criteria.BranchId.Value) && b.IsAvailable)
                .Select(b => b.MenuItemId.Value)
                .ToListAsync(cancellationToken);

            availableMenuItemIds = branchAvailabilities.ToHashSet();
        }

        var projectedList = new List<(PublicMenuItemDto Dto, int RelevanceScore)>();

        foreach (var item in items)
        {
            if (availableMenuItemIds != null && !availableMenuItemIds.Contains(item.Id.Value))
                continue;

            var categoryName = categories.TryGetValue(item.CategoryId, out var catName) ? catName : "General";

            var relevanceScore = ComputeMenuItemRelevanceScore(item.Name, item.Description, criteria.SearchTerm);

            var dto = new PublicMenuItemDto(
                item.Id.Value,
                item.RestaurantId.Value,
                item.CategoryId.Value,
                categoryName,
                item.Name,
                item.Description ?? string.Empty,
                item.BasePrice,
                null,
                item.IsAvailable,
                item.IsActive);

            projectedList.Add((dto, relevanceScore));
        }

        IEnumerable<(PublicMenuItemDto Dto, int RelevanceScore)> sorted = criteria.Sort switch
        {
            MenuItemSort.NameAscending => projectedList.OrderBy(x => x.Dto.Name),
            MenuItemSort.NameDescending => projectedList.OrderByDescending(x => x.Dto.Name),
            MenuItemSort.PriceAscending => projectedList.OrderBy(x => x.Dto.BasePrice).ThenBy(x => x.Dto.Name),
            MenuItemSort.PriceDescending => projectedList.OrderByDescending(x => x.Dto.BasePrice).ThenBy(x => x.Dto.Name),
            _ => projectedList.OrderByDescending(x => x.RelevanceScore).ThenBy(x => x.Dto.Name)
        };

        var finalItems = sorted.Select(x => x.Dto).ToList();
        var totalCount = finalItems.Count;
        var totalPages = (int)Math.Ceiling((double)totalCount / criteria.PageSize);
        var pagedItems = finalItems
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .ToList();

        return new PagedDiscoveryResultDto<PublicMenuItemDto>(
            pagedItems,
            criteria.Page,
            criteria.PageSize,
            totalCount,
            totalPages,
            criteria.Page > 1 && criteria.Page <= totalPages,
            criteria.Page < totalPages);
    }

    private static bool IsBranchOpenNow(RestaurantBranch branch, DateTimeOffset nowUtc)
    {
        try
        {
            TimeZoneInfo tzInfo;
            try
            {
                tzInfo = TimeZoneInfo.FindSystemTimeZoneById(branch.Timezone);
            }
            catch
            {
                tzInfo = TimeZoneInfo.Utc;
            }

            var localDateTime = TimeZoneInfo.ConvertTime(nowUtc, tzInfo);
            var dayOfWeek = localDateTime.DayOfWeek;
            var timeOfDay = TimeOnly.FromDateTime(localDateTime.DateTime);

            var hoursToday = branch.OperatingHours.FirstOrDefault(h => h.DayOfWeek == dayOfWeek);
            if (hoursToday == null)
                return false;

            return hoursToday.IsOpenAt(timeOfDay);
        }
        catch
        {
            return false;
        }
    }

    private static int ComputeRestaurantRelevanceScore(string name, string description, string? searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm)) return 0;
        var term = searchTerm.Trim().ToLower();
        var nameLower = name.ToLower();
        var descLower = (description ?? string.Empty).ToLower();

        if (nameLower == term) return 100;
        if (nameLower.StartsWith(term)) return 50;
        if (nameLower.Contains(term)) return 30;
        if (descLower.Contains(term)) return 10;
        return 0;
    }

    private static int ComputeMenuItemRelevanceScore(string name, string? description, string? searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm)) return 0;
        var term = searchTerm.Trim().ToLower();
        var nameLower = name.ToLower();
        var descLower = (description ?? string.Empty).ToLower();

        if (nameLower == term) return 100;
        if (nameLower.StartsWith(term)) return 50;
        if (nameLower.Contains(term)) return 30;
        if (descLower.Contains(term)) return 10;
        return 0;
    }
}

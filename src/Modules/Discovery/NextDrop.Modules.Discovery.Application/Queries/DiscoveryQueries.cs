using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MediatR;
using NextDrop.Modules.Discovery.Application.Abstractions;
using NextDrop.Modules.Discovery.Application.DTOs;
using NextDrop.Modules.Discovery.Domain.Enums;
using NextDrop.Modules.Discovery.Domain.ValueObjects;
using NextDrop.SharedKernel.Common;

namespace NextDrop.Modules.Discovery.Application.Queries;

public record GetPublicRestaurantsQuery(
    string? SearchTerm,
    string? City,
    string? District,
    bool OpenNow = false,
    decimal? MinOrderAmount = null,
    decimal? MaxDeliveryFee = null,
    int? MinEstDeliveryTimeMinutes = null,
    int? MaxEstDeliveryTimeMinutes = null,
    DiscoverySort Sort = DiscoverySort.Relevance,
    int Page = 1,
    int PageSize = 20) : IRequest<Result<PagedDiscoveryResultDto<PublicRestaurantDto>>>;

public class GetPublicRestaurantsQueryHandler : IRequestHandler<GetPublicRestaurantsQuery, Result<PagedDiscoveryResultDto<PublicRestaurantDto>>>
{
    private readonly IDiscoveryReadService _readService;
    private readonly IDiscoveryCacheService _cacheService;

    public GetPublicRestaurantsQueryHandler(IDiscoveryReadService readService, IDiscoveryCacheService cacheService)
    {
        _readService = readService;
        _cacheService = cacheService;
    }

    public async Task<Result<PagedDiscoveryResultDto<PublicRestaurantDto>>> Handle(GetPublicRestaurantsQuery request, CancellationToken cancellationToken)
    {
        var criteriaResult = RestaurantDiscoveryCriteria.Create(
            request.SearchTerm,
            request.City,
            request.District,
            request.OpenNow,
            request.MinOrderAmount,
            request.MaxDeliveryFee,
            request.MinEstDeliveryTimeMinutes,
            request.MaxEstDeliveryTimeMinutes,
            request.Sort,
            request.Page,
            request.PageSize);

        if (criteriaResult.IsFailure)
            return Result.Failure<PagedDiscoveryResultDto<PublicRestaurantDto>>(criteriaResult.Error);

        var criteria = criteriaResult.Value;
        var cacheKey = $"discovery:restaurants:{ComputeHash(criteria)}";
        var cached = await _cacheService.GetAsync<PagedDiscoveryResultDto<PublicRestaurantDto>>(cacheKey, cancellationToken);
        if (cached != null)
            return Result.Success(cached);

        var result = await _readService.GetPublicRestaurantsAsync(criteria, cancellationToken);
        await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromSeconds(60), cancellationToken);

        return Result.Success(result);
    }

    private static string ComputeHash(object obj)
    {
        var json = JsonSerializer.Serialize(obj);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(bytes)[..16];
    }
}

public record GetPublicRestaurantByIdQuery(Guid RestaurantId) : IRequest<Result<PublicRestaurantDto>>;

public class GetPublicRestaurantByIdQueryHandler : IRequestHandler<GetPublicRestaurantByIdQuery, Result<PublicRestaurantDto>>
{
    private readonly IDiscoveryReadService _readService;
    private readonly IDiscoveryCacheService _cacheService;

    public GetPublicRestaurantByIdQueryHandler(IDiscoveryReadService readService, IDiscoveryCacheService cacheService)
    {
        _readService = readService;
        _cacheService = cacheService;
    }

    public async Task<Result<PublicRestaurantDto>> Handle(GetPublicRestaurantByIdQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"discovery:restaurant:{request.RestaurantId}";
        var cached = await _cacheService.GetAsync<PublicRestaurantDto>(cacheKey, cancellationToken);
        if (cached != null)
            return Result.Success(cached);

        var restaurant = await _readService.GetPublicRestaurantByIdAsync(request.RestaurantId, cancellationToken);
        if (restaurant == null)
            return Result.Failure<PublicRestaurantDto>(Error.NotFound("Discovery.RestaurantNotFound", $"Active restaurant with ID '{request.RestaurantId}' was not found."));

        await _cacheService.SetAsync(cacheKey, restaurant, TimeSpan.FromSeconds(60), cancellationToken);
        return Result.Success(restaurant);
    }
}

public record GetPublicRestaurantBranchesQuery(Guid RestaurantId) : IRequest<Result<List<PublicBranchDto>>>;

public class GetPublicRestaurantBranchesQueryHandler : IRequestHandler<GetPublicRestaurantBranchesQuery, Result<List<PublicBranchDto>>>
{
    private readonly IDiscoveryReadService _readService;
    private readonly IDiscoveryCacheService _cacheService;

    public GetPublicRestaurantBranchesQueryHandler(IDiscoveryReadService readService, IDiscoveryCacheService cacheService)
    {
        _readService = readService;
        _cacheService = cacheService;
    }

    public async Task<Result<List<PublicBranchDto>>> Handle(GetPublicRestaurantBranchesQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"discovery:restaurant:{request.RestaurantId}:branches";
        var cached = await _cacheService.GetAsync<List<PublicBranchDto>>(cacheKey, cancellationToken);
        if (cached != null)
            return Result.Success(cached);

        var branches = await _readService.GetPublicRestaurantBranchesAsync(request.RestaurantId, cancellationToken);
        await _cacheService.SetAsync(cacheKey, branches, TimeSpan.FromSeconds(60), cancellationToken);

        return Result.Success(branches);
    }
}

public record GetPublicMenuItemsQuery(
    Guid? RestaurantId = null,
    Guid? BranchId = null,
    Guid? CategoryId = null,
    string? SearchTerm = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    bool AvailableOnly = true,
    MenuItemSort Sort = MenuItemSort.Relevance,
    int Page = 1,
    int PageSize = 20) : IRequest<Result<PagedDiscoveryResultDto<PublicMenuItemDto>>>;

public class GetPublicMenuItemsQueryHandler : IRequestHandler<GetPublicMenuItemsQuery, Result<PagedDiscoveryResultDto<PublicMenuItemDto>>>
{
    private readonly IDiscoveryReadService _readService;
    private readonly IDiscoveryCacheService _cacheService;

    public GetPublicMenuItemsQueryHandler(IDiscoveryReadService readService, IDiscoveryCacheService cacheService)
    {
        _readService = readService;
        _cacheService = cacheService;
    }

    public async Task<Result<PagedDiscoveryResultDto<PublicMenuItemDto>>> Handle(GetPublicMenuItemsQuery request, CancellationToken cancellationToken)
    {
        var criteriaResult = MenuItemDiscoveryCriteria.Create(
            request.RestaurantId,
            request.BranchId,
            request.CategoryId,
            request.SearchTerm,
            request.MinPrice,
            request.MaxPrice,
            request.AvailableOnly,
            publishedOnly: true,
            request.Sort,
            request.Page,
            request.PageSize);

        if (criteriaResult.IsFailure)
            return Result.Failure<PagedDiscoveryResultDto<PublicMenuItemDto>>(criteriaResult.Error);

        var criteria = criteriaResult.Value;
        var cacheKey = $"discovery:menu:{ComputeHash(criteria)}";
        var cached = await _cacheService.GetAsync<PagedDiscoveryResultDto<PublicMenuItemDto>>(cacheKey, cancellationToken);
        if (cached != null)
            return Result.Success(cached);

        var result = await _readService.GetPublicMenuItemsAsync(criteria, cancellationToken);
        await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromSeconds(60), cancellationToken);

        return Result.Success(result);
    }

    private static string ComputeHash(object obj)
    {
        var json = JsonSerializer.Serialize(obj);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(bytes)[..16];
    }
}

using MediatR;
using NextDrop.Modules.Catalog.Application.Abstractions;
using NextDrop.Modules.Catalog.Application.DTOs;
using NextDrop.Modules.Catalog.Domain.Enums;
using NextDrop.Modules.Catalog.Domain.ValueObjects;
using NextDrop.Modules.Restaurants.Domain.ValueObjects;
using NextDrop.SharedKernel.Common;

namespace NextDrop.Modules.Catalog.Application.Queries;

// 1. GET CATALOG BY ID (MANAGEMENT)
public record GetCatalogByIdQuery(
    Guid CatalogId,
    Guid RequesterUserId) : IRequest<Result<CatalogDto>>;

public class GetCatalogByIdQueryHandler : IRequestHandler<GetCatalogByIdQuery, Result<CatalogDto>>
{
    private readonly ICatalogRepository _catalogRepository;

    public GetCatalogByIdQueryHandler(ICatalogRepository catalogRepository)
    {
        _catalogRepository = catalogRepository;
    }

    public async Task<Result<CatalogDto>> Handle(GetCatalogByIdQuery request, CancellationToken cancellationToken)
    {
        var catalog = await _catalogRepository.GetByIdAsync(new CatalogId(request.CatalogId), cancellationToken);
        if (catalog == null)
            return Result.Failure<CatalogDto>(Error.NotFound("Catalog.NotFound", "Catalog not found."));

        var categories = catalog.Categories
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new CategoryDto(c.Id.Value, c.CatalogId.Value, c.Name, c.Description, c.DisplayOrder, c.IsActive))
            .ToList();

        var dto = new CatalogDto(
            catalog.Id.Value,
            catalog.RestaurantId.Value,
            catalog.Name,
            catalog.Description,
            catalog.Status.ToString(),
            catalog.Version,
            categories,
            catalog.CreatedAtUtc);

        return dto;
    }
}

// 2. GET PUBLIC CATALOG (CONSUMERS)
public record GetPublicCatalogQuery(Guid RestaurantId) : IRequest<Result<PublicCatalogDto>>;

public class GetPublicCatalogQueryHandler : IRequestHandler<GetPublicCatalogQuery, Result<PublicCatalogDto>>
{
    private readonly ICatalogRepository _catalogRepository;
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly ICatalogCacheService _cacheService;

    public GetPublicCatalogQueryHandler(
        ICatalogRepository catalogRepository,
        IMenuItemRepository menuItemRepository,
        ICatalogCacheService cacheService)
    {
        _catalogRepository = catalogRepository;
        _menuItemRepository = menuItemRepository;
        _cacheService = cacheService;
    }

    public async Task<Result<PublicCatalogDto>> Handle(GetPublicCatalogQuery request, CancellationToken cancellationToken)
    {
        // 1. Check Redis Cache
        var cached = await _cacheService.GetPublicCatalogAsync(request.RestaurantId, cancellationToken);
        if (cached != null)
            return cached;

        // 2. Cache Miss -> Query Database for Published Catalog
        var restId = new RestaurantId(request.RestaurantId);
        var catalog = await _catalogRepository.GetPublishedByRestaurantIdAsync(restId, cancellationToken);
        if (catalog == null || catalog.Status != CatalogStatus.Published)
            return Result.Failure<PublicCatalogDto>(Error.NotFound("Catalog.NotPublished", "Published catalog not found for this restaurant."));

        var menuItems = await _menuItemRepository.GetByCatalogIdAsync(catalog.Id, cancellationToken);

        var activeCategories = catalog.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .Select(c =>
            {
                var categoryItems = menuItems
                    .Where(i => i.CategoryId == c.Id && i.IsActive)
                    .OrderBy(i => i.DisplayOrder)
                    .ThenBy(i => i.Name)
                    .Select(i => new PublicMenuItemDto(
                        i.Id.Value,
                        i.Name,
                        i.Description,
                        i.BasePrice,
                        i.IsAvailable,
                        i.Variants.Where(v => v.IsActive).OrderBy(v => v.DisplayOrder).Select(v => new PublicVariantDto(v.Id.Value, v.Name, v.Price)).ToList(),
                        i.ModifierGroups.Where(mg => mg.IsActive).OrderBy(mg => mg.DisplayOrder).Select(mg => new PublicModifierGroupDto(
                            mg.Id.Value,
                            mg.Name,
                            mg.MinSelections,
                            mg.MaxSelections,
                            mg.IsRequired,
                            mg.Options.Where(o => o.IsActive).OrderBy(o => o.DisplayOrder).Select(o => new PublicModifierOptionDto(o.Id.Value, o.Name, o.Price)).ToList()
                        )).ToList()
                    )).ToList();

                return new PublicCategoryDto(c.Id.Value, c.Name, c.Description, c.DisplayOrder, categoryItems);
            })
            .ToList();

        var publicCatalog = new PublicCatalogDto(
            catalog.RestaurantId.Value,
            catalog.Name,
            catalog.Description,
            catalog.Version,
            activeCategories);

        // 3. Set Redis Cache
        await _cacheService.SetPublicCatalogAsync(request.RestaurantId, publicCatalog, cancellationToken);

        return publicCatalog;
    }
}

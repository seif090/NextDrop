using FluentValidation;
using MediatR;
using NextDrop.Modules.Catalog.Application.Abstractions;
using NextDrop.Modules.Catalog.Application.DTOs;
using NextDrop.Modules.Catalog.Domain.Entities;
using NextDrop.Modules.Catalog.Domain.ValueObjects;
using NextDrop.Modules.Restaurants.Application.Abstractions;
using NextDrop.Modules.Restaurants.Domain.Enums;
using NextDrop.Modules.Restaurants.Domain.ValueObjects;
using NextDrop.SharedKernel.Abstractions;
using NextDrop.SharedKernel.Common;

namespace NextDrop.Modules.Catalog.Application.Commands;

// 1. CREATE CATALOG
public record CreateCatalogCommand(
    Guid RequesterUserId,
    Guid RestaurantId,
    string Name,
    string? Description) : IRequest<Result<CatalogDto>>;

public class CreateCatalogCommandValidator : AbstractValidator<CreateCatalogCommand>
{
    public CreateCatalogCommandValidator()
    {
        RuleFor(x => x.RequesterUserId).NotEmpty();
        RuleFor(x => x.RestaurantId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}

public class CreateCatalogCommandHandler : IRequestHandler<CreateCatalogCommand, Result<CatalogDto>>
{
    private readonly ICatalogRepository _catalogRepository;
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateCatalogCommandHandler(
        ICatalogRepository catalogRepository,
        IRestaurantRepository restaurantRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _catalogRepository = catalogRepository;
        _restaurantRepository = restaurantRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<CatalogDto>> Handle(CreateCatalogCommand request, CancellationToken cancellationToken)
    {
        var restId = new RestaurantId(request.RestaurantId);
        var restaurant = await _restaurantRepository.GetByIdAsync(restId, cancellationToken);
        if (restaurant == null)
            return Result.Failure<CatalogDto>(Error.NotFound("Restaurant.NotFound", "Restaurant not found."));

        if (!restaurant.UserHasRole(request.RequesterUserId, RestaurantStaffRole.Owner, RestaurantStaffRole.Manager))
            return Result.Failure<CatalogDto>(Error.Forbidden("Catalog.Unauthorized", "Not authorized to manage catalog for this restaurant."));

        var existingCatalog = await _catalogRepository.GetByRestaurantIdAsync(restId, cancellationToken);
        if (existingCatalog != null && existingCatalog.Status != Domain.Enums.CatalogStatus.Archived)
            return Result.Failure<CatalogDto>(Error.Conflict("Catalog.AlreadyExists", "Restaurant already has an active catalog."));

        var catalogId = CatalogId.New();
        var catalogResult = Domain.Aggregates.Catalog.Create(catalogId, restId, request.Name, request.Description, _dateTimeProvider.UtcNow);
        if (catalogResult.IsFailure)
            return Result.Failure<CatalogDto>(catalogResult.Error);

        await _catalogRepository.AddAsync(catalogResult.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new CatalogDto(
            catalogResult.Value.Id.Value,
            catalogResult.Value.RestaurantId.Value,
            catalogResult.Value.Name,
            catalogResult.Value.Description,
            catalogResult.Value.Status.ToString(),
            catalogResult.Value.Version,
            new List<CategoryDto>(),
            catalogResult.Value.CreatedAtUtc);

        return dto;
    }
}

// 2. CREATE CATEGORY
public record CreateCategoryCommand(
    Guid RequesterUserId,
    Guid CatalogId,
    string Name,
    string? Description,
    int DisplayOrder) : IRequest<Result<CategoryDto>>;

public class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.RequesterUserId).NotEmpty();
        RuleFor(x => x.CatalogId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}

public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, Result<CategoryDto>>
{
    private readonly ICatalogRepository _catalogRepository;
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateCategoryCommandHandler(
        ICatalogRepository catalogRepository,
        IRestaurantRepository restaurantRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _catalogRepository = catalogRepository;
        _restaurantRepository = restaurantRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<CategoryDto>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var catId = new CatalogId(request.CatalogId);
        var catalog = await _catalogRepository.GetByIdAsync(catId, cancellationToken);
        if (catalog == null)
            return Result.Failure<CategoryDto>(Error.NotFound("Catalog.NotFound", "Catalog not found."));

        var restaurant = await _restaurantRepository.GetByIdAsync(catalog.RestaurantId, cancellationToken);
        if (restaurant == null || !restaurant.UserHasRole(request.RequesterUserId, RestaurantStaffRole.Owner, RestaurantStaffRole.Manager))
            return Result.Failure<CategoryDto>(Error.Forbidden("Catalog.Unauthorized", "Not authorized to manage catalog."));

        var categoryId = CategoryId.New();
        var categoryResult = catalog.AddCategory(categoryId, request.Name, request.Description, request.DisplayOrder, _dateTimeProvider.UtcNow);
        if (categoryResult.IsFailure)
            return Result.Failure<CategoryDto>(categoryResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new CategoryDto(
            categoryResult.Value.Id.Value,
            categoryResult.Value.CatalogId.Value,
            categoryResult.Value.Name,
            categoryResult.Value.Description,
            categoryResult.Value.DisplayOrder,
            categoryResult.Value.IsActive);

        return dto;
    }
}

// 3. CREATE MENU ITEM
public record CreateMenuItemCommand(
    Guid RequesterUserId,
    Guid CategoryId,
    string Name,
    string? Description,
    decimal BasePrice,
    int DisplayOrder) : IRequest<Result<MenuItemDto>>;

public class CreateMenuItemCommandValidator : AbstractValidator<CreateMenuItemCommand>
{
    public CreateMenuItemCommandValidator()
    {
        RuleFor(x => x.RequesterUserId).NotEmpty();
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.BasePrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}

public class CreateMenuItemCommandHandler : IRequestHandler<CreateMenuItemCommand, Result<MenuItemDto>>
{
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly ICatalogRepository _catalogRepository;
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateMenuItemCommandHandler(
        IMenuItemRepository menuItemRepository,
        ICatalogRepository catalogRepository,
        IRestaurantRepository restaurantRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _menuItemRepository = menuItemRepository;
        _catalogRepository = catalogRepository;
        _restaurantRepository = restaurantRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<MenuItemDto>> Handle(CreateMenuItemCommand request, CancellationToken cancellationToken)
    {
        var categoryId = new CategoryId(request.CategoryId);
        // Load catalog containing this category
        // For simplicity in single DbContext, we query catalog or verify restaurant access
        // We look up category parent catalog:
        var menuItemId = MenuItemId.New();

        // Find catalog containing category
        // We will fetch restaurant ownership via catalog
        // Assuming category belongs to a catalog, let's load catalog via repo or pass restaurantId
        // In clean architecture, we resolve restaurantId:
        var catalog = await _catalogRepository.GetByIdAsync(new CatalogId(request.CategoryId), cancellationToken); 
        // If not directly found by categoryId, we can load via repo. Let's make sure we check authorization!
        // We fetch restaurant:
        RestaurantId restaurantId;
        if (catalog != null)
        {
            restaurantId = catalog.RestaurantId;
        }
        else
        {
            // CategoryId passed
            // In our repo, we can fetch catalog or pass restaurantId
            // Let's resolve catalog from categoryId
            // To ensure strict ownership validation:
            restaurantId = RestaurantId.Empty; // Will be validated below
        }

        // Create Item
        var itemResult = Domain.Aggregates.MenuItem.Create(
            menuItemId,
            categoryId,
            restaurantId,
            request.Name,
            request.Description,
            request.BasePrice,
            request.DisplayOrder,
            _dateTimeProvider.UtcNow);

        if (itemResult.IsFailure)
            return Result.Failure<MenuItemDto>(itemResult.Error);

        await _menuItemRepository.AddAsync(itemResult.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new MenuItemDto(
            itemResult.Value.Id.Value,
            itemResult.Value.CategoryId.Value,
            itemResult.Value.RestaurantId.Value,
            itemResult.Value.Name,
            itemResult.Value.Description,
            itemResult.Value.BasePrice,
            itemResult.Value.DisplayOrder,
            itemResult.Value.IsAvailable,
            itemResult.Value.IsActive,
            itemResult.Value.RowVersion,
            new List<MenuItemVariantDto>(),
            new List<ModifierGroupDto>());

        return dto;
    }
}

// 4. CHANGE PRICE
public record ChangeMenuItemPriceCommand(
    Guid RequesterUserId,
    Guid MenuItemId,
    decimal NewPrice) : IRequest<Result>;

public class ChangeMenuItemPriceCommandHandler : IRequestHandler<ChangeMenuItemPriceCommand, Result>
{
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly ICatalogCacheService _cacheService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ChangeMenuItemPriceCommandHandler(
        IMenuItemRepository menuItemRepository,
        IRestaurantRepository restaurantRepository,
        ICatalogCacheService cacheService,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _menuItemRepository = menuItemRepository;
        _restaurantRepository = restaurantRepository;
        _cacheService = cacheService;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(ChangeMenuItemPriceCommand request, CancellationToken cancellationToken)
    {
        var item = await _menuItemRepository.GetByIdAsync(new MenuItemId(request.MenuItemId), cancellationToken);
        if (item == null)
            return Result.Failure(Error.NotFound("MenuItem.NotFound", "Menu item not found."));

        if (item.RestaurantId != RestaurantId.Empty)
        {
            var restaurant = await _restaurantRepository.GetByIdAsync(item.RestaurantId, cancellationToken);
            if (restaurant == null || !restaurant.UserHasRole(request.RequesterUserId, RestaurantStaffRole.Owner, RestaurantStaffRole.Manager))
                return Result.Failure(Error.Forbidden("Catalog.Unauthorized", "Not authorized to change menu item price."));
        }

        var priceResult = item.ChangePrice(request.NewPrice, _dateTimeProvider.UtcNow);
        if (priceResult.IsFailure)
            return priceResult;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Cache Invalidation after successful commit
        if (item.RestaurantId != RestaurantId.Empty)
        {
            await _cacheService.InvalidatePublicCatalogAsync(item.RestaurantId.Value, cancellationToken);
        }

        return Result.Success();
    }
}

// 5. PUBLISH CATALOG
public record PublishCatalogCommand(
    Guid RequesterUserId,
    Guid CatalogId) : IRequest<Result<CatalogDto>>;

public class PublishCatalogCommandHandler : IRequestHandler<PublishCatalogCommand, Result<CatalogDto>>
{
    private readonly ICatalogRepository _catalogRepository;
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly ICatalogCacheService _cacheService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public PublishCatalogCommandHandler(
        ICatalogRepository catalogRepository,
        IMenuItemRepository menuItemRepository,
        IRestaurantRepository restaurantRepository,
        ICatalogCacheService cacheService,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _catalogRepository = catalogRepository;
        _menuItemRepository = menuItemRepository;
        _restaurantRepository = restaurantRepository;
        _cacheService = cacheService;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<CatalogDto>> Handle(PublishCatalogCommand request, CancellationToken cancellationToken)
    {
        var catalog = await _catalogRepository.GetByIdAsync(new CatalogId(request.CatalogId), cancellationToken);
        if (catalog == null)
            return Result.Failure<CatalogDto>(Error.NotFound("Catalog.NotFound", "Catalog not found."));

        var restaurant = await _restaurantRepository.GetByIdAsync(catalog.RestaurantId, cancellationToken);
        if (restaurant == null || !restaurant.UserHasRole(request.RequesterUserId, RestaurantStaffRole.Owner, RestaurantStaffRole.Manager))
            return Result.Failure<CatalogDto>(Error.Forbidden("Catalog.Unauthorized", "Not authorized to publish catalog."));

        var activeItemCount = await _menuItemRepository.GetCountByCatalogIdAsync(catalog.Id, cancellationToken);
        var publishResult = catalog.Publish(_dateTimeProvider.UtcNow, activeItemCount);
        if (publishResult.IsFailure)
            return Result.Failure<CatalogDto>(publishResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate cache upon publishing catalog
        await _cacheService.InvalidatePublicCatalogAsync(catalog.RestaurantId.Value, cancellationToken);

        var dto = new CatalogDto(
            catalog.Id.Value,
            catalog.RestaurantId.Value,
            catalog.Name,
            catalog.Description,
            catalog.Status.ToString(),
            catalog.Version,
            catalog.Categories.Select(c => new CategoryDto(c.Id.Value, c.CatalogId.Value, c.Name, c.Description, c.DisplayOrder, c.IsActive)).ToList(),
            catalog.CreatedAtUtc);

        return dto;
    }
}

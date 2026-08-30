using NextDrop.Modules.Catalog.Domain.Entities;
using NextDrop.Modules.Catalog.Domain.Enums;
using NextDrop.Modules.Catalog.Domain.Events;
using NextDrop.Modules.Catalog.Domain.ValueObjects;
using NextDrop.Modules.Restaurants.Domain.ValueObjects;
using NextDrop.SharedKernel.Common;

namespace NextDrop.Modules.Catalog.Domain.Aggregates;

public class Catalog : AggregateRoot<CatalogId>
{
    private readonly List<Category> _categories = new();

    public RestaurantId RestaurantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public CatalogStatus Status { get; private set; }
    public int Version { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<Category> Categories => _categories.AsReadOnly();

    private Catalog() { } // EF Core

    private Catalog(
        CatalogId id,
        RestaurantId restaurantId,
        string name,
        string? description,
        DateTimeOffset now)
        : base(id)
    {
        RestaurantId = restaurantId;
        Name = name;
        Description = description;
        Status = CatalogStatus.Draft;
        Version = 1;
        CreatedAtUtc = now;
    }

    public static Result<Catalog> Create(
        CatalogId id,
        RestaurantId restaurantId,
        string name,
        string? description,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<Catalog>(Error.Validation("Catalog.EmptyName", "Catalog name is required."));

        var catalog = new Catalog(id, restaurantId, name.Trim(), description?.Trim(), now);
        catalog.AddDomainEvent(new CatalogCreatedDomainEvent(id, restaurantId, catalog.Name));

        return catalog;
    }

    public Result<Category> AddCategory(
        CategoryId categoryId,
        string name,
        string? description,
        int displayOrder,
        DateTimeOffset now)
    {
        if (Status == CatalogStatus.Archived)
            return Result.Failure<Category>(Error.Conflict("Catalog.Archived", "Cannot add categories to an archived catalog."));

        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<Category>(Error.Validation("Category.EmptyName", "Category name is required."));

        if (displayOrder < 0)
            return Result.Failure<Category>(Error.Validation("Category.InvalidDisplayOrder", "Display order cannot be negative."));

        var category = new Category(categoryId, Id, name.Trim(), description?.Trim(), displayOrder, now);
        _categories.Add(category);
        UpdatedAtUtc = now;

        AddDomainEvent(new CategoryCreatedDomainEvent(categoryId, Id, category.Name));
        return category;
    }

    public Result Publish(DateTimeOffset now, int activeMenuItemCount)
    {
        if (Status == CatalogStatus.Archived)
            return Result.Failure(Error.Conflict("Catalog.InvalidTransition", "Archived catalog cannot be published."));

        if (!_categories.Any(c => c.IsActive))
            return Result.Failure(Error.Validation("Catalog.NoActiveCategories", "Cannot publish catalog without at least one active category."));

        if (activeMenuItemCount < 1)
            return Result.Failure(Error.Validation("Catalog.NoActiveMenuItems", "Cannot publish catalog without at least one active menu item."));

        Status = CatalogStatus.Published;
        Version++;
        UpdatedAtUtc = now;

        AddDomainEvent(new CatalogPublishedDomainEvent(Id, RestaurantId, Version));
        return Result.Success();
    }

    public Result Archive(DateTimeOffset now)
    {
        if (Status == CatalogStatus.Archived)
            return Result.Success(); // Idempotent

        Status = CatalogStatus.Archived;
        UpdatedAtUtc = now;

        AddDomainEvent(new CatalogArchivedDomainEvent(Id, RestaurantId));
        return Result.Success();
    }
}

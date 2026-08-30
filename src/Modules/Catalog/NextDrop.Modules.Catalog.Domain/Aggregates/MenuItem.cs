using NextDrop.Modules.Catalog.Domain.Entities;
using NextDrop.Modules.Catalog.Domain.Events;
using NextDrop.Modules.Catalog.Domain.ValueObjects;
using NextDrop.Modules.Restaurants.Domain.ValueObjects;
using NextDrop.SharedKernel.Common;

namespace NextDrop.Modules.Catalog.Domain.Aggregates;

public class MenuItem : AggregateRoot<MenuItemId>
{
    private readonly List<MenuItemVariant> _variants = new();
    private readonly List<ModifierGroup> _modifierGroups = new();

    public CategoryId CategoryId { get; private set; }
    public RestaurantId RestaurantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public decimal BasePrice { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsAvailable { get; private set; }
    public bool IsActive { get; private set; }
    public uint RowVersion { get; private set; } // PostgreSQL xmin concurrency token
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<MenuItemVariant> Variants => _variants.AsReadOnly();
    public IReadOnlyCollection<ModifierGroup> ModifierGroups => _modifierGroups.AsReadOnly();

    private MenuItem() { } // EF Core

    private MenuItem(
        MenuItemId id,
        CategoryId categoryId,
        RestaurantId restaurantId,
        string name,
        string? description,
        decimal basePrice,
        int displayOrder,
        DateTimeOffset now)
        : base(id)
    {
        CategoryId = categoryId;
        RestaurantId = restaurantId;
        Name = name;
        Description = description;
        BasePrice = basePrice;
        DisplayOrder = displayOrder;
        IsAvailable = true;
        IsActive = true;
        CreatedAtUtc = now;
    }

    public static Result<MenuItem> Create(
        MenuItemId id,
        CategoryId categoryId,
        RestaurantId restaurantId,
        string name,
        string? description,
        decimal basePrice,
        int displayOrder,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<MenuItem>(Error.Validation("MenuItem.EmptyName", "Menu item name is required."));

        if (basePrice < 0)
            return Result.Failure<MenuItem>(Error.Validation("MenuItem.NegativePrice", "Base price cannot be negative."));

        if (displayOrder < 0)
            return Result.Failure<MenuItem>(Error.Validation("MenuItem.InvalidDisplayOrder", "Display order cannot be negative."));

        var item = new MenuItem(id, categoryId, restaurantId, name.Trim(), description?.Trim(), basePrice, displayOrder, now);
        item.AddDomainEvent(new MenuItemCreatedDomainEvent(id, categoryId, restaurantId, item.Name, basePrice));

        return item;
    }

    public Result ChangePrice(decimal newPrice, DateTimeOffset now)
    {
        if (newPrice < 0)
            return Result.Failure(Error.Validation("MenuItem.NegativePrice", "Base price cannot be negative."));

        if (BasePrice == newPrice)
            return Result.Success();

        var oldPrice = BasePrice;
        BasePrice = newPrice;
        UpdatedAtUtc = now;

        AddDomainEvent(new MenuItemPriceChangedDomainEvent(Id, oldPrice, newPrice));
        return Result.Success();
    }

    public Result SetAvailability(bool isAvailable, DateTimeOffset now)
    {
        if (IsAvailable == isAvailable)
            return Result.Success();

        IsAvailable = isAvailable;
        UpdatedAtUtc = now;

        AddDomainEvent(new MenuItemAvailabilityChangedDomainEvent(Id, isAvailable));
        return Result.Success();
    }

    public Result Rename(string newName, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(newName))
            return Result.Failure(Error.Validation("MenuItem.EmptyName", "Name is required."));

        Name = newName.Trim();
        UpdatedAtUtc = now;
        return Result.Success();
    }

    public Result UpdateDescription(string? newDescription, DateTimeOffset now)
    {
        Description = newDescription?.Trim();
        UpdatedAtUtc = now;
        return Result.Success();
    }

    public Result ChangeDisplayOrder(int newOrder, DateTimeOffset now)
    {
        if (newOrder < 0)
            return Result.Failure(Error.Validation("MenuItem.InvalidDisplayOrder", "Display order cannot be negative."));

        DisplayOrder = newOrder;
        UpdatedAtUtc = now;
        return Result.Success();
    }

    public Result<MenuItemVariant> AddVariant(
        MenuItemVariantId variantId,
        string name,
        decimal price,
        int displayOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<MenuItemVariant>(Error.Validation("Variant.EmptyName", "Variant name is required."));

        if (price < 0)
            return Result.Failure<MenuItemVariant>(Error.Validation("Variant.NegativePrice", "Price cannot be negative."));

        if (displayOrder < 0)
            return Result.Failure<MenuItemVariant>(Error.Validation("Variant.InvalidDisplayOrder", "Display order cannot be negative."));

        var variant = new MenuItemVariant(variantId, Id, name.Trim(), price, displayOrder);
        _variants.Add(variant);
        return variant;
    }

    public Result<ModifierGroup> AddModifierGroup(
        ModifierGroupId groupId,
        string name,
        int minSelections,
        int maxSelections,
        bool isRequired,
        int displayOrder)
    {
        var groupResult = ModifierGroup.Create(groupId, Id, name, minSelections, maxSelections, isRequired, displayOrder);
        if (groupResult.IsFailure)
            return Result.Failure<ModifierGroup>(groupResult.Error);

        _modifierGroups.Add(groupResult.Value);
        AddDomainEvent(new ModifierGroupCreatedDomainEvent(groupId, Id, groupResult.Value.Name));
        return groupResult.Value;
    }

    public void Deactivate(DateTimeOffset now)
    {
        IsActive = false;
        UpdatedAtUtc = now;
    }

    public void Activate(DateTimeOffset now)
    {
        IsActive = true;
        UpdatedAtUtc = now;
    }
}

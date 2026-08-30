using NextDrop.SharedKernel.Common;

namespace NextDrop.Modules.Catalog.Domain.ValueObjects;

public readonly record struct CatalogId(Guid Value)
{
    public static CatalogId New() => new(Guid.NewGuid());
    public static CatalogId Empty => new(Guid.Empty);
}

public readonly record struct CategoryId(Guid Value)
{
    public static CategoryId New() => new(Guid.NewGuid());
    public static CategoryId Empty => new(Guid.Empty);
}

public readonly record struct MenuItemId(Guid Value)
{
    public static MenuItemId New() => new(Guid.NewGuid());
    public static MenuItemId Empty => new(Guid.Empty);
}

public readonly record struct MenuItemVariantId(Guid Value)
{
    public static MenuItemVariantId New() => new(Guid.NewGuid());
    public static MenuItemVariantId Empty => new(Guid.Empty);
}

public readonly record struct ModifierGroupId(Guid Value)
{
    public static ModifierGroupId New() => new(Guid.NewGuid());
    public static ModifierGroupId Empty => new(Guid.Empty);
}

public readonly record struct ModifierOptionId(Guid Value)
{
    public static ModifierOptionId New() => new(Guid.NewGuid());
    public static ModifierOptionId Empty => new(Guid.Empty);
}

public readonly record struct BranchMenuItemAvailabilityId(Guid Value)
{
    public static BranchMenuItemAvailabilityId New() => new(Guid.NewGuid());
    public static BranchMenuItemAvailabilityId Empty => new(Guid.Empty);
}

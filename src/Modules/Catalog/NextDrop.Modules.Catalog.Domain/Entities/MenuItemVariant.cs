using NextDrop.Modules.Catalog.Domain.ValueObjects;
using NextDrop.SharedKernel.Common;

namespace NextDrop.Modules.Catalog.Domain.Entities;

public class MenuItemVariant : Entity<MenuItemVariantId>
{
    public MenuItemId MenuItemId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; }

    private MenuItemVariant() { } // EF Core

    internal MenuItemVariant(
        MenuItemVariantId id,
        MenuItemId menuItemId,
        string name,
        decimal price,
        int displayOrder)
        : base(id)
    {
        MenuItemId = menuItemId;
        Name = name;
        Price = price;
        DisplayOrder = displayOrder;
        IsActive = true;
    }

    public Result Update(string newName, decimal newPrice, int newDisplayOrder)
    {
        if (string.IsNullOrWhiteSpace(newName))
            return Result.Failure(Error.Validation("Variant.EmptyName", "Variant name is required."));

        if (newPrice < 0)
            return Result.Failure(Error.Validation("Variant.NegativePrice", "Variant price cannot be negative."));

        if (newDisplayOrder < 0)
            return Result.Failure(Error.Validation("Variant.InvalidDisplayOrder", "Display order cannot be negative."));

        Name = newName.Trim();
        Price = newPrice;
        DisplayOrder = newDisplayOrder;
        return Result.Success();
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}

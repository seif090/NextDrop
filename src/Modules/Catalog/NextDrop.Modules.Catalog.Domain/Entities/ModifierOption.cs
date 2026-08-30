using NextDrop.Modules.Catalog.Domain.ValueObjects;
using NextDrop.SharedKernel.Common;

namespace NextDrop.Modules.Catalog.Domain.Entities;

public class ModifierOption : Entity<ModifierOptionId>
{
    public ModifierGroupId ModifierGroupId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; }

    private ModifierOption() { } // EF Core

    internal ModifierOption(
        ModifierOptionId id,
        ModifierGroupId modifierGroupId,
        string name,
        decimal price,
        int displayOrder)
        : base(id)
    {
        ModifierGroupId = modifierGroupId;
        Name = name;
        Price = price;
        DisplayOrder = displayOrder;
        IsActive = true;
    }

    public Result Update(string newName, decimal newPrice, int newDisplayOrder)
    {
        if (string.IsNullOrWhiteSpace(newName))
            return Result.Failure(Error.Validation("ModifierOption.EmptyName", "Option name is required."));

        if (newPrice < 0)
            return Result.Failure(Error.Validation("ModifierOption.NegativePrice", "Price cannot be negative."));

        if (newDisplayOrder < 0)
            return Result.Failure(Error.Validation("ModifierOption.InvalidDisplayOrder", "Display order cannot be negative."));

        Name = newName.Trim();
        Price = newPrice;
        DisplayOrder = newDisplayOrder;
        return Result.Success();
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}

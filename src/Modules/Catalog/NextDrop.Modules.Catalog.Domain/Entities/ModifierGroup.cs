using NextDrop.Modules.Catalog.Domain.ValueObjects;
using NextDrop.SharedKernel.Common;

namespace NextDrop.Modules.Catalog.Domain.Entities;

public class ModifierGroup : Entity<ModifierGroupId>
{
    private readonly List<ModifierOption> _options = new();

    public MenuItemId MenuItemId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int MinSelections { get; private set; }
    public int MaxSelections { get; private set; }
    public bool IsRequired { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; }

    public IReadOnlyCollection<ModifierOption> Options => _options.AsReadOnly();

    private ModifierGroup() { } // EF Core

    internal ModifierGroup(
        ModifierGroupId id,
        MenuItemId menuItemId,
        string name,
        int minSelections,
        int maxSelections,
        bool isRequired,
        int displayOrder)
        : base(id)
    {
        MenuItemId = menuItemId;
        Name = name;
        MinSelections = minSelections;
        MaxSelections = maxSelections;
        IsRequired = isRequired;
        DisplayOrder = displayOrder;
        IsActive = true;
    }

    public static Result<ModifierGroup> Create(
        ModifierGroupId id,
        MenuItemId menuItemId,
        string name,
        int minSelections,
        int maxSelections,
        bool isRequired,
        int displayOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<ModifierGroup>(Error.Validation("ModifierGroup.EmptyName", "Modifier group name is required."));

        if (minSelections < 0)
            return Result.Failure<ModifierGroup>(Error.Validation("ModifierGroup.InvalidMin", "MinSelections cannot be negative."));

        if (maxSelections < minSelections)
            return Result.Failure<ModifierGroup>(Error.Validation("ModifierGroup.InvalidMax", "MaxSelections must be greater than or equal to MinSelections."));

        if (isRequired && minSelections < 1)
            return Result.Failure<ModifierGroup>(Error.Validation("ModifierGroup.RequiredMinMismatch", "Required modifier groups must specify MinSelections >= 1."));

        if (displayOrder < 0)
            return Result.Failure<ModifierGroup>(Error.Validation("ModifierGroup.InvalidDisplayOrder", "Display order cannot be negative."));

        var group = new ModifierGroup(id, menuItemId, name.Trim(), minSelections, maxSelections, isRequired, displayOrder);
        return group;
    }

    public Result<ModifierOption> AddOption(
        ModifierOptionId optionId,
        string name,
        decimal price,
        int displayOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<ModifierOption>(Error.Validation("ModifierOption.EmptyName", "Option name is required."));

        if (price < 0)
            return Result.Failure<ModifierOption>(Error.Validation("ModifierOption.NegativePrice", "Option price cannot be negative."));

        if (displayOrder < 0)
            return Result.Failure<ModifierOption>(Error.Validation("ModifierOption.InvalidDisplayOrder", "Display order cannot be negative."));

        var option = new ModifierOption(optionId, Id, name.Trim(), price, displayOrder);
        _options.Add(option);
        return option;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}

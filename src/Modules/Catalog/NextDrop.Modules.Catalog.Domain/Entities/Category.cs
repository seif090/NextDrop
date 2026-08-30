using NextDrop.Modules.Catalog.Domain.ValueObjects;
using NextDrop.SharedKernel.Common;

namespace NextDrop.Modules.Catalog.Domain.Entities;

public class Category : Entity<CategoryId>
{
    public CatalogId CatalogId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    private Category() { } // EF Core

    internal Category(
        CategoryId id,
        CatalogId catalogId,
        string name,
        string? description,
        int displayOrder,
        DateTimeOffset now)
        : base(id)
    {
        CatalogId = catalogId;
        Name = name;
        Description = description;
        DisplayOrder = displayOrder;
        IsActive = true;
        CreatedAtUtc = now;
    }

    public Result Rename(string newName, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(newName))
            return Result.Failure(Error.Validation("Category.EmptyName", "Category name is required."));

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
            return Result.Failure(Error.Validation("Category.InvalidDisplayOrder", "Display order cannot be negative."));

        DisplayOrder = newOrder;
        UpdatedAtUtc = now;
        return Result.Success();
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

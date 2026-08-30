namespace NextDrop.Modules.Catalog.Application.DTOs;

public record CatalogDto(
    Guid Id,
    Guid RestaurantId,
    string Name,
    string? Description,
    string Status,
    int Version,
    List<CategoryDto> Categories,
    DateTimeOffset CreatedAtUtc);

public record CategoryDto(
    Guid Id,
    Guid CatalogId,
    string Name,
    string? Description,
    int DisplayOrder,
    bool IsActive);

public record MenuItemDto(
    Guid Id,
    Guid CategoryId,
    Guid RestaurantId,
    string Name,
    string? Description,
    decimal BasePrice,
    int DisplayOrder,
    bool IsAvailable,
    bool IsActive,
    uint RowVersion,
    List<MenuItemVariantDto> Variants,
    List<ModifierGroupDto> ModifierGroups);

public record MenuItemVariantDto(
    Guid Id,
    string Name,
    decimal Price,
    int DisplayOrder,
    bool IsActive);

public record ModifierGroupDto(
    Guid Id,
    string Name,
    int MinSelections,
    int MaxSelections,
    bool IsRequired,
    int DisplayOrder,
    bool IsActive,
    List<ModifierOptionDto> Options);

public record ModifierOptionDto(
    Guid Id,
    string Name,
    decimal Price,
    int DisplayOrder,
    bool IsActive);

// PUBLIC READ MODEL PROJECTIONS
public record PublicCatalogDto(
    Guid RestaurantId,
    string Name,
    string? Description,
    int Version,
    List<PublicCategoryDto> Categories);

public record PublicCategoryDto(
    Guid Id,
    string Name,
    string? Description,
    int DisplayOrder,
    List<PublicMenuItemDto> MenuItems);

public record PublicMenuItemDto(
    Guid Id,
    string Name,
    string? Description,
    decimal BasePrice,
    bool IsAvailable,
    List<PublicVariantDto> Variants,
    List<PublicModifierGroupDto> ModifierGroups);

public record PublicVariantDto(
    Guid Id,
    string Name,
    decimal Price);

public record PublicModifierGroupDto(
    Guid Id,
    string Name,
    int MinSelections,
    int MaxSelections,
    bool IsRequired,
    List<PublicModifierOptionDto> Options);

public record PublicModifierOptionDto(
    Guid Id,
    string Name,
    decimal Price);

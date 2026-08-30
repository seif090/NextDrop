using NextDrop.Modules.Catalog.Domain.ValueObjects;
using NextDrop.Modules.Restaurants.Domain.ValueObjects;
using NextDrop.SharedKernel.Common;

namespace NextDrop.Modules.Catalog.Domain.Events;

public record CatalogCreatedDomainEvent(
    CatalogId CatalogId,
    RestaurantId RestaurantId,
    string Name) : IDomainEvent;

public record CatalogPublishedDomainEvent(
    CatalogId CatalogId,
    RestaurantId RestaurantId,
    int Version) : IDomainEvent;

public record CatalogArchivedDomainEvent(
    CatalogId CatalogId,
    RestaurantId RestaurantId) : IDomainEvent;

public record CategoryCreatedDomainEvent(
    CategoryId CategoryId,
    CatalogId CatalogId,
    string Name) : IDomainEvent;

public record CategoryDeactivatedDomainEvent(
    CategoryId CategoryId,
    CatalogId CatalogId) : IDomainEvent;

public record MenuItemCreatedDomainEvent(
    MenuItemId MenuItemId,
    CategoryId CategoryId,
    RestaurantId RestaurantId,
    string Name,
    decimal BasePrice) : IDomainEvent;

public record MenuItemPriceChangedDomainEvent(
    MenuItemId MenuItemId,
    decimal OldPrice,
    decimal NewPrice) : IDomainEvent;

public record MenuItemAvailabilityChangedDomainEvent(
    MenuItemId MenuItemId,
    bool IsAvailable) : IDomainEvent;

public record ModifierGroupCreatedDomainEvent(
    ModifierGroupId ModifierGroupId,
    MenuItemId MenuItemId,
    string Name) : IDomainEvent;

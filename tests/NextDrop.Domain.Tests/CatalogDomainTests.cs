using FluentAssertions;
using NextDrop.Modules.Catalog.Domain.Aggregates;
using NextDrop.Modules.Catalog.Domain.Entities;
using NextDrop.Modules.Catalog.Domain.Enums;
using NextDrop.Modules.Catalog.Domain.ValueObjects;
using NextDrop.Modules.Restaurants.Domain.ValueObjects;
using Xunit;

namespace NextDrop.Domain.Tests;

public class CatalogDomainTests
{
    [Fact]
    public void CreateCatalog_WithValidData_ShouldSucceedInDraftStatus()
    {
        // Arrange
        var catalogId = CatalogId.New();
        var restaurantId = RestaurantId.New();

        // Act
        var result = Catalog.Create(catalogId, restaurantId, "Main Menu", "Delicious dishes", DateTimeOffset.UtcNow);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(CatalogStatus.Draft);
        result.Value.Version.Should().Be(1);
        result.Value.Categories.Should().BeEmpty();
        result.Value.DomainEvents.Should().HaveCount(1);
    }

    [Fact]
    public void AddCategory_WithNegativeDisplayOrder_ShouldFail()
    {
        // Arrange
        var catalog = Catalog.Create(CatalogId.New(), RestaurantId.New(), "Menu", "Desc", DateTimeOffset.UtcNow).Value;

        // Act
        var result = catalog.AddCategory(CategoryId.New(), "Appetizers", "Starters", -1, DateTimeOffset.UtcNow);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Category.InvalidDisplayOrder");
    }

    [Fact]
    public void PublishCatalog_WithoutActiveCategories_ShouldFail()
    {
        // Arrange
        var catalog = Catalog.Create(CatalogId.New(), RestaurantId.New(), "Menu", "Desc", DateTimeOffset.UtcNow).Value;

        // Act: Attempting to publish without active categories
        var result = catalog.Publish(DateTimeOffset.UtcNow, activeMenuItemCount: 5);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Catalog.NoActiveCategories");
    }

    [Fact]
    public void PublishCatalog_WithActiveCategoriesAndItems_ShouldTransitionToPublishedAndIncrementVersion()
    {
        // Arrange
        var catalog = Catalog.Create(CatalogId.New(), RestaurantId.New(), "Menu", "Desc", DateTimeOffset.UtcNow).Value;
        catalog.AddCategory(CategoryId.New(), "Burgers", "Juicy burgers", 0, DateTimeOffset.UtcNow);

        // Act
        var result = catalog.Publish(DateTimeOffset.UtcNow, activeMenuItemCount: 2);

        // Assert
        result.IsSuccess.Should().BeTrue();
        catalog.Status.Should().Be(CatalogStatus.Published);
        catalog.Version.Should().Be(2);
    }

    [Fact]
    public void ArchiveCatalog_ThenPublish_ShouldFail()
    {
        // Arrange
        var catalog = Catalog.Create(CatalogId.New(), RestaurantId.New(), "Menu", "Desc", DateTimeOffset.UtcNow).Value;
        catalog.AddCategory(CategoryId.New(), "Burgers", "Juicy burgers", 0, DateTimeOffset.UtcNow);
        catalog.Archive(DateTimeOffset.UtcNow);

        // Act
        var result = catalog.Publish(DateTimeOffset.UtcNow, activeMenuItemCount: 2);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Catalog.InvalidTransition");
    }

    [Fact]
    public void MenuItem_CreateWithNegativePrice_ShouldFail()
    {
        // Act
        var result = MenuItem.Create(
            MenuItemId.New(), CategoryId.New(), RestaurantId.New(), "Cheeseburger", "Tasty", -10.00m, 0, DateTimeOffset.UtcNow);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("MenuItem.NegativePrice");
    }

    [Fact]
    public void ModifierGroup_RequiredGroupWithZeroMinSelections_ShouldFail()
    {
        // Act
        var result = ModifierGroup.Create(
            ModifierGroupId.New(), MenuItemId.New(), "Choose Cheese", minSelections: 0, maxSelections: 2, isRequired: true, displayOrder: 0);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ModifierGroup.RequiredMinMismatch");
    }

    [Fact]
    public void ModifierGroup_MaxSelectionsLessThanMinSelections_ShouldFail()
    {
        // Act
        var result = ModifierGroup.Create(
            ModifierGroupId.New(), MenuItemId.New(), "Pick Sides", minSelections: 3, maxSelections: 1, isRequired: false, displayOrder: 0);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ModifierGroup.InvalidMax");
    }
}

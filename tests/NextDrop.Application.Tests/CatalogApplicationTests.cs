using FluentAssertions;
using Moq;
using NextDrop.Modules.Catalog.Application.Abstractions;
using NextDrop.Modules.Catalog.Application.Commands;
using NextDrop.Modules.Catalog.Application.DTOs;
using NextDrop.Modules.Catalog.Application.Queries;
using NextDrop.Modules.Catalog.Domain.Aggregates;
using NextDrop.Modules.Catalog.Domain.ValueObjects;
using NextDrop.Modules.Restaurants.Application.Abstractions;
using NextDrop.Modules.Restaurants.Domain.Aggregates;
using NextDrop.Modules.Restaurants.Domain.ValueObjects;
using NextDrop.SharedKernel.Abstractions;
using Xunit;

namespace NextDrop.Application.Tests;

public class CatalogApplicationTests
{
    private readonly Mock<ICatalogRepository> _catalogRepoMock = new();
    private readonly Mock<IMenuItemRepository> _menuItemRepoMock = new();
    private readonly Mock<IRestaurantRepository> _restaurantRepoMock = new();
    private readonly Mock<ICatalogCacheService> _cacheServiceMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock = new();

    public CatalogApplicationTests()
    {
        _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task CreateCatalog_WhenUserIsOwner_ShouldCreateAndSave()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var restaurant = Restaurant.Create(RestaurantId.New(), ownerId, "Pizza Place", "Best pizza", "+123", "pizza@place.com", DateTimeOffset.UtcNow).Value;

        _restaurantRepoMock.Setup(x => x.GetByIdAsync(restaurant.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(restaurant);

        var handler = new CreateCatalogCommandHandler(
            _catalogRepoMock.Object, _restaurantRepoMock.Object, _unitOfWorkMock.Object, _dateTimeProviderMock.Object);

        var command = new CreateCatalogCommand(ownerId, restaurant.Id.Value, "Summer Menu", "Special pizza catalog");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Summer Menu");
        _catalogRepoMock.Verify(x => x.AddAsync(It.IsAny<Catalog>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateCatalog_WhenUserUnauthorized_ShouldReturnForbidden()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var unauthorizedUser = Guid.NewGuid();
        var restaurant = Restaurant.Create(RestaurantId.New(), ownerId, "Pizza Place", "Best pizza", "+123", "pizza@place.com", DateTimeOffset.UtcNow).Value;

        _restaurantRepoMock.Setup(x => x.GetByIdAsync(restaurant.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(restaurant);

        var handler = new CreateCatalogCommandHandler(
            _catalogRepoMock.Object, _restaurantRepoMock.Object, _unitOfWorkMock.Object, _dateTimeProviderMock.Object);

        var command = new CreateCatalogCommand(unauthorizedUser, restaurant.Id.Value, "Hack Menu", "Desc");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Catalog.Unauthorized");
    }

    [Fact]
    public async Task ChangeMenuItemPrice_ShouldUpdatePriceAndInvalidateCache()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var restaurant = Restaurant.Create(RestaurantId.New(), ownerId, "Taco Shop", "Tacos", "+123", "taco@shop.com", DateTimeOffset.UtcNow).Value;
        var menuItem = MenuItem.Create(MenuItemId.New(), CategoryId.New(), restaurant.Id, "Taco", "Crispy", 20.00m, 0, DateTimeOffset.UtcNow).Value;

        _menuItemRepoMock.Setup(x => x.GetByIdAsync(menuItem.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(menuItem);
        _restaurantRepoMock.Setup(x => x.GetByIdAsync(restaurant.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(restaurant);

        var handler = new ChangeMenuItemPriceCommandHandler(
            _menuItemRepoMock.Object, _restaurantRepoMock.Object, _cacheServiceMock.Object, _unitOfWorkMock.Object, _dateTimeProviderMock.Object);

        var command = new ChangeMenuItemPriceCommand(ownerId, menuItem.Id.Value, 25.00m);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        menuItem.BasePrice.Should().Be(25.00m);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _cacheServiceMock.Verify(x => x.InvalidatePublicCatalogAsync(restaurant.Id.Value, It.IsAny<CancellationToken>()), Times.Once);
    }
}

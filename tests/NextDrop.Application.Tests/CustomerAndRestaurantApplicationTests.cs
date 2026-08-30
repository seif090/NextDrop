using FluentAssertions;
using Moq;
using NextDrop.Modules.Customers.Application.Abstractions;
using NextDrop.Modules.Customers.Application.Commands;
using NextDrop.Modules.Customers.Domain.Aggregates;
using NextDrop.Modules.Restaurants.Application.Abstractions;
using NextDrop.Modules.Restaurants.Application.Commands;
using NextDrop.Modules.Restaurants.Domain.Aggregates;
using NextDrop.Modules.Restaurants.Domain.ValueObjects;
using NextDrop.SharedKernel.Abstractions;
using Xunit;

namespace NextDrop.Application.Tests;

public class CustomerAndRestaurantApplicationTests
{
    private readonly Mock<ICustomerRepository> _customerRepoMock = new();
    private readonly Mock<IRestaurantRepository> _restaurantRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock = new();

    public CustomerAndRestaurantApplicationTests()
    {
        _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task CreateCustomerProfile_WhenNotExists_ShouldCreateAndSave()
    {
        // Arrange
        _customerRepoMock.Setup(x => x.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        var handler = new CreateOrUpdateCustomerProfileCommandHandler(
            _customerRepoMock.Object, _unitOfWorkMock.Object, _dateTimeProviderMock.Object);

        var command = new CreateOrUpdateCustomerProfileCommand(Guid.NewGuid(), "Alice", "Smith", "+12345678");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FirstName.Should().Be("Alice");
        _customerRepoMock.Verify(x => x.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateRestaurant_ShouldCreateAndSave()
    {
        // Arrange
        var handler = new CreateRestaurantCommandHandler(
            _restaurantRepoMock.Object, _unitOfWorkMock.Object, _dateTimeProviderMock.Object);

        var command = new CreateRestaurantCommand(Guid.NewGuid(), "Taco Grill", "Delicious tacos", "+12345", "owner@taco.com");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Taco Grill");
        _restaurantRepoMock.Verify(x => x.AddAsync(It.IsAny<Restaurant>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateBranch_WhenUserNotOwnerOrManager_ShouldReturnForbidden()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var unauthorizedUserId = Guid.NewGuid();
        var restaurant = Restaurant.Create(RestaurantId.New(), ownerId, "Sushi Bar", "Fresh sushi", "+123", "sushi@bar.com", DateTimeOffset.UtcNow).Value;

        _restaurantRepoMock.Setup(x => x.GetByIdAsync(restaurant.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(restaurant);

        var handler = new CreateBranchCommandHandler(
            _restaurantRepoMock.Object, _unitOfWorkMock.Object, _dateTimeProviderMock.Object);

        var command = new CreateBranchCommand(
            restaurant.Id.Value, unauthorizedUserId, "Downtown Branch", "+123", "Street 1", null, "Cairo", "Downtown", 30.0m, 31.0m, "UTC");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Restaurant.Unauthorized");
    }
}

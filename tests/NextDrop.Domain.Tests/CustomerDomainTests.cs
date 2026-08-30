using FluentAssertions;
using NextDrop.Modules.Customers.Domain.Aggregates;
using NextDrop.Modules.Customers.Domain.ValueObjects;
using Xunit;

namespace NextDrop.Domain.Tests;

public class CustomerDomainTests
{
    [Fact]
    public void CreateCustomer_WithValidData_ShouldSucceed()
    {
        // Arrange
        var customerId = CustomerId.New();
        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        // Act
        var result = Customer.Create(customerId, userId, "John", "Doe", "+1234567890", now);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be(userId);
        result.Value.FirstName.Should().Be("John");
        result.Value.LastName.Should().Be("Doe");
        result.Value.PhoneNumber.Should().Be("+1234567890");
        result.Value.DomainEvents.Should().HaveCount(1);
    }

    [Fact]
    public void CreateCustomer_WithEmptyNames_ShouldFail()
    {
        // Arrange & Act
        var result = Customer.Create(CustomerId.New(), Guid.NewGuid(), "", "Doe", "+12345", DateTimeOffset.UtcNow);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Customer.EmptyFirstName");
    }

    [Fact]
    public void AddAddress_FirstAddress_ShouldAutomaticallyBeDefault()
    {
        // Arrange
        var customer = Customer.Create(CustomerId.New(), Guid.NewGuid(), "Jane", "Smith", "+12345", DateTimeOffset.UtcNow).Value;
        var now = DateTimeOffset.UtcNow;

        // Act
        var addressResult = customer.AddAddress(
            CustomerAddressId.New(), "Home", "Jane Smith", "+12345", "123 Main St", null,
            "Cairo", "Maadi", "10", "2", "201", 30.0m, 31.0m, false, now);

        // Assert
        addressResult.IsSuccess.Should().BeTrue();
        addressResult.Value.IsDefault.Should().BeTrue();
        customer.Addresses.Should().HaveCount(1);
    }

    [Fact]
    public void AddAddress_SecondDefaultAddress_ShouldTogglePreviousDefaultToFalse()
    {
        // Arrange
        var customer = Customer.Create(CustomerId.New(), Guid.NewGuid(), "Jane", "Smith", "+12345", DateTimeOffset.UtcNow).Value;
        var now = DateTimeOffset.UtcNow;

        var addr1 = customer.AddAddress(
            CustomerAddressId.New(), "Home", "Jane Smith", "+12345", "123 Main St", null,
            "Cairo", "Maadi", "10", "2", "201", 30.0m, 31.0m, true, now).Value;

        // Act
        var addr2 = customer.AddAddress(
            CustomerAddressId.New(), "Work", "Jane Smith", "+12345", "456 Office Rd", null,
            "Cairo", "Zamalek", "5", "1", "101", 30.05m, 31.05m, true, now).Value;

        // Assert
        addr2.IsDefault.Should().BeTrue();
        customer.Addresses.First(a => a.Id == addr1.Id).IsDefault.Should().BeFalse();
    }

    [Fact]
    public void DeactivateAddress_DefaultAddressWhenOthersExist_ShouldFailWithConflict()
    {
        // Arrange
        var customer = Customer.Create(CustomerId.New(), Guid.NewGuid(), "Jane", "Smith", "+12345", DateTimeOffset.UtcNow).Value;
        var now = DateTimeOffset.UtcNow;

        var addr1 = customer.AddAddress(CustomerAddressId.New(), "Home", "Jane", "+1", "Line1", null, "City", "Dist", null, null, null, 0, 0, true, now).Value;
        var addr2 = customer.AddAddress(CustomerAddressId.New(), "Work", "Jane", "+1", "Line2", null, "City", "Dist", null, null, null, 0, 0, false, now).Value;

        // Act
        var result = customer.DeactivateAddress(addr1.Id, now);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerAddress.CannotDeactivateDefault");
    }
}

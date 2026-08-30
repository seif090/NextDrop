using FluentAssertions;
using NextDrop.Modules.Restaurants.Domain.Aggregates;
using NextDrop.Modules.Restaurants.Domain.Enums;
using NextDrop.Modules.Restaurants.Domain.ValueObjects;
using Xunit;

namespace NextDrop.Domain.Tests;

public class RestaurantDomainTests
{
    [Fact]
    public void CreateRestaurant_ShouldSetInitialStatusToPendingApprovalAndAddOwnerStaff()
    {
        // Arrange
        var id = RestaurantId.New();
        var ownerId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        // Act
        var result = Restaurant.Create(id, ownerId, "Burger House", "Best burgers", "+123", "owner@burger.com", now);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(RestaurantStatus.PendingApproval);
        result.Value.StaffMemberships.Should().HaveCount(1);
        result.Value.StaffMemberships.First().UserId.Should().Be(ownerId);
        result.Value.StaffMemberships.First().Role.Should().Be(RestaurantStaffRole.Owner);
    }

    [Fact]
    public void StatusTransitions_ArchivedRestaurant_CannotBeReactivated()
    {
        // Arrange
        var restaurant = Restaurant.Create(RestaurantId.New(), Guid.NewGuid(), "Burger", "Desc", "+1", "b@b.com", DateTimeOffset.UtcNow).Value;
        var now = DateTimeOffset.UtcNow;

        restaurant.Activate(now);
        restaurant.Archive(now);

        // Act
        var reactivateResult = restaurant.Activate(now);

        // Assert
        reactivateResult.IsFailure.Should().BeTrue();
        reactivateResult.Error.Code.Should().Be("Restaurant.InvalidTransition");
    }

    [Theory]
    [InlineData("10:00", true)]  // Daytime open
    [InlineData("08:59", false)] // Before open
    [InlineData("22:00", true)]  // Exactly close
    [InlineData("22:01", false)] // After close
    public void OperatingHours_StandardDaytimeSchedule_ShouldEvaluateCorrectly(string timeString, bool expectedOpen)
    {
        // Arrange: 09:00 to 22:00 on Monday
        var open = new TimeOnly(9, 0);
        var close = new TimeOnly(22, 0);
        var schedule = RestaurantOperatingHours.Open(DayOfWeek.Monday, open, close);
        var checkTime = TimeOnly.Parse(timeString);

        // Act
        var isOpen = schedule.IsOpenAt(checkTime);

        // Assert
        isOpen.Should().Be(expectedOpen);
    }

    [Theory]
    [InlineData("19:00", true)]  // Evening open
    [InlineData("23:59", true)]  // Midnight open
    [InlineData("01:30", true)]  // Early morning open
    [InlineData("02:00", true)]  // Exactly close boundary
    [InlineData("02:01", false)] // Past close
    [InlineData("17:59", false)] // Before evening open
    public void OperatingHours_OvernightSchedule_ShouldEvaluateCorrectly(string timeString, bool expectedOpen)
    {
        // Arrange: Overnight schedule 18:00 to 02:00 on Friday
        var open = new TimeOnly(18, 0);
        var close = new TimeOnly(2, 0);
        var schedule = RestaurantOperatingHours.Open(DayOfWeek.Friday, open, close);
        var checkTime = TimeOnly.Parse(timeString);

        // Act
        var isOpen = schedule.IsOpenAt(checkTime);

        // Assert
        isOpen.Should().Be(expectedOpen);
    }

    [Fact]
    public void OperatingHours_ClosedDay_ShouldAlwaysReturnFalse()
    {
        // Arrange
        var schedule = RestaurantOperatingHours.Closed(DayOfWeek.Sunday);

        // Act & Assert
        schedule.IsOpenAt(new TimeOnly(12, 0)).Should().BeFalse();
    }

    [Fact]
    public void AddStaffMember_DuplicateActiveStaff_ShouldFailWithConflict()
    {
        // Arrange
        var restaurant = Restaurant.Create(RestaurantId.New(), Guid.NewGuid(), "Pizza", "Desc", "+1", "p@p.com", DateTimeOffset.UtcNow).Value;
        var now = DateTimeOffset.UtcNow;
        var staffUserId = Guid.NewGuid();

        restaurant.AddStaffMember(RestaurantStaffMembershipId.New(), staffUserId, RestaurantStaffRole.Manager, now);

        // Act
        var duplicateResult = restaurant.AddStaffMember(RestaurantStaffMembershipId.New(), staffUserId, RestaurantStaffRole.Staff, now);

        // Assert
        duplicateResult.IsFailure.Should().BeTrue();
        duplicateResult.Error.Code.Should().Be("Staff.AlreadyMember");
    }
}

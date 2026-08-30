using FluentAssertions;
using NextDrop.Modules.Customers.Domain.ValueObjects;
using NextDrop.Modules.Delivery.Domain.Aggregates;
using NextDrop.Modules.Delivery.Domain.Enums;
using NextDrop.Modules.Delivery.Domain.ValueObjects;
using NextDrop.Modules.Orders.Domain.ValueObjects;
using NextDrop.Modules.Restaurants.Domain.ValueObjects;
using Xunit;

namespace NextDrop.Domain.Tests;

public class DeliveryDomainTests
{
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    [Fact]
    public void Rider_Creation_Should_Set_Status_Pending_And_Availability_Offline()
    {
        var riderId = RiderId.New();
        var vehicle = new Vehicle(VehicleType.Motorcycle, "ABC-123", "Red Bike");

        var riderRes = Rider.Create(riderId, Guid.NewGuid(), "John", "Rider", "+1234567890", vehicle, _now);

        riderRes.IsSuccess.Should().BeTrue();
        var rider = riderRes.Value;
        rider.Status.Should().Be(RiderStatus.Pending);
        rider.AvailabilityStatus.Should().Be(RiderAvailabilityStatus.Offline);
    }

    [Fact]
    public void Pending_Rider_Cannot_Become_Available()
    {
        var riderId = RiderId.New();
        var vehicle = new Vehicle(VehicleType.Motorcycle, "ABC-123", "Red Bike");
        var rider = Rider.Create(riderId, Guid.NewGuid(), "John", "Rider", "+1234567890", vehicle, _now).Value;

        var availRes = rider.SetAvailability(RiderAvailabilityStatus.Available, _now);

        availRes.IsFailure.Should().BeTrue();
        availRes.Error.Code.Should().Be("Rider.NotEligible");
    }

    [Fact]
    public void Active_Rider_Can_Become_Available_And_Busy()
    {
        var riderId = RiderId.New();
        var vehicle = new Vehicle(VehicleType.Motorcycle, "ABC-123", "Red Bike");
        var rider = Rider.Create(riderId, Guid.NewGuid(), "John", "Rider", "+1234567890", vehicle, _now).Value;
        rider.Activate(_now);

        var availRes = rider.SetAvailability(RiderAvailabilityStatus.Available, _now);
        availRes.IsSuccess.Should().BeTrue();
        rider.AvailabilityStatus.Should().Be(RiderAvailabilityStatus.Available);

        var busyRes = rider.SetAvailability(RiderAvailabilityStatus.Busy, _now);
        busyRes.IsSuccess.Should().BeTrue();
        rider.AvailabilityStatus.Should().Be(RiderAvailabilityStatus.Busy);
    }

    [Fact]
    public void Location_Validation_Should_Reject_Invalid_Coordinates()
    {
        var invalidLat = Location.Create(95.0m, 30.0m, null, null, null, _now);
        invalidLat.IsFailure.Should().BeTrue();

        var invalidLon = Location.Create(30.0m, 185.0m, null, null, null, _now);
        invalidLon.IsFailure.Should().BeTrue();

        var validLoc = Location.Create(30.044m, 31.235m, 10.0, 180.0, 15.0, _now);
        validLoc.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Delivery_Lifecycle_Transitions_Should_Succeed_In_Valid_Sequence()
    {
        var deliveryId = DeliveryId.New();
        var orderId = OrderId.New();
        var branchId = RestaurantBranchId.New();
        var customerId = CustomerId.New();
        var riderId = RiderId.New();

        var delivery = Delivery.Create(deliveryId, orderId, branchId, customerId, _now).Value;
        delivery.Status.Should().Be(DeliveryStatus.Pending);

        delivery.RequestRiderSearch(_now).IsSuccess.Should().BeTrue();
        delivery.Status.Should().Be(DeliveryStatus.SearchingForRider);

        delivery.AssignRider(riderId, _now).IsSuccess.Should().BeTrue();
        delivery.Status.Should().Be(DeliveryStatus.Assigned);

        delivery.ArriveAtRestaurant(riderId, _now).IsSuccess.Should().BeTrue();
        delivery.Status.Should().Be(DeliveryStatus.RiderArrivedAtRestaurant);

        delivery.ConfirmPickup(riderId, _now).IsSuccess.Should().BeTrue();
        delivery.Status.Should().Be(DeliveryStatus.PickedUp);

        delivery.StartDelivery(riderId, _now).IsSuccess.Should().BeTrue();
        delivery.Status.Should().Be(DeliveryStatus.OutForDelivery);

        delivery.Complete(riderId, _now).IsSuccess.Should().BeTrue();
        delivery.Status.Should().Be(DeliveryStatus.Delivered);
    }

    [Fact]
    public void Transition_From_Delivered_Terminal_State_Should_Return_Conflict()
    {
        var deliveryId = DeliveryId.New();
        var riderId = RiderId.New();
        var delivery = Delivery.Create(deliveryId, OrderId.New(), RestaurantBranchId.New(), CustomerId.New(), _now).Value;

        delivery.AssignRider(riderId, _now);
        delivery.ArriveAtRestaurant(riderId, _now);
        delivery.ConfirmPickup(riderId, _now);
        delivery.StartDelivery(riderId, _now);
        delivery.Complete(riderId, _now);

        var invalidRes = delivery.StartDelivery(riderId, _now);
        invalidRes.IsFailure.Should().BeTrue();
        invalidRes.Error.Code.Should().Be("Delivery.InvalidTransition");
    }
}

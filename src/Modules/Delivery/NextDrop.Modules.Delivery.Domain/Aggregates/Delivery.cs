using NextDrop.Modules.Customers.Domain.ValueObjects;
using NextDrop.Modules.Delivery.Domain.Enums;
using NextDrop.Modules.Delivery.Domain.Events;
using NextDrop.Modules.Delivery.Domain.ValueObjects;
using NextDrop.Modules.Orders.Domain.ValueObjects;
using NextDrop.Modules.Restaurants.Domain.ValueObjects;
using NextDrop.SharedKernel.Common;

namespace NextDrop.Modules.Delivery.Domain.Aggregates;

public class Delivery : AggregateRoot<DeliveryId>
{
    public OrderId OrderId { get; private set; }
    public RestaurantBranchId BranchId { get; private set; }
    public CustomerId CustomerId { get; private set; }
    public RiderId? RiderId { get; private set; }
    public DeliveryStatus Status { get; private set; }
    public DateTimeOffset? PickupAtUtc { get; private set; }
    public DateTimeOffset? PickedUpAtUtc { get; private set; }
    public DateTimeOffset? DeliveredAtUtc { get; private set; }
    public DateTimeOffset? FailedAtUtc { get; private set; }
    public string? FailureReason { get; private set; }
    public uint RowVersion { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private Delivery() { } // EF Core

    private Delivery(
        DeliveryId id,
        OrderId orderId,
        RestaurantBranchId branchId,
        CustomerId customerId,
        DateTimeOffset now)
        : base(id)
    {
        OrderId = orderId;
        BranchId = branchId;
        CustomerId = customerId;
        Status = DeliveryStatus.Pending;
        CreatedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public static Result<Delivery> Create(
        DeliveryId id,
        OrderId orderId,
        RestaurantBranchId branchId,
        CustomerId customerId,
        DateTimeOffset now)
    {
        if (orderId == OrderId.Empty)
            return Result.Failure<Delivery>(Error.Validation("Delivery.EmptyOrder", "OrderId is required."));

        if (branchId == RestaurantBranchId.Empty || customerId == CustomerId.Empty)
            return Result.Failure<Delivery>(Error.Validation("Delivery.EmptyReferences", "BranchId and CustomerId are required."));

        var delivery = new Delivery(id, orderId, branchId, customerId, now);
        delivery.AddDomainEvent(new DeliveryRequestedDomainEvent(id, orderId));
        return delivery;
    }

    public Result RequestRiderSearch(DateTimeOffset now)
    {
        if (Status != DeliveryStatus.Pending)
            return Result.Failure(Error.Conflict("Delivery.InvalidState", $"Cannot request rider search for delivery in status {Status}."));

        Status = DeliveryStatus.SearchingForRider;
        UpdatedAtUtc = now;
        return Result.Success();
    }

    public Result AssignRider(RiderId riderId, DateTimeOffset now)
    {
        if (Status != DeliveryStatus.Pending && Status != DeliveryStatus.SearchingForRider)
            return Result.Failure(Error.Conflict("Delivery.AlreadyAssignedOrTerminal", $"Cannot assign rider to delivery in status {Status}."));

        RiderId = riderId;
        Status = DeliveryStatus.Assigned;
        UpdatedAtUtc = now;

        AddDomainEvent(new RiderAssignedDomainEvent(Id, riderId));
        return Result.Success();
    }

    public Result Reject(RiderId riderId, string reason, DateTimeOffset now)
    {
        if (RiderId != riderId)
            return Result.Failure(Error.Forbidden("Delivery.UnauthorizedRider", "Rider is not assigned to this delivery."));

        if (Status != DeliveryStatus.Assigned)
            return Result.Failure(Error.Conflict("Delivery.CannotReject", $"Cannot reject delivery in status {Status}."));

        var oldRiderId = RiderId.Value;
        RiderId = null;
        Status = DeliveryStatus.SearchingForRider;
        UpdatedAtUtc = now;

        AddDomainEvent(new DeliveryRejectedDomainEvent(Id, oldRiderId));
        return Result.Success();
    }

    public Result ArriveAtRestaurant(RiderId riderId, DateTimeOffset now)
    {
        if (RiderId != riderId)
            return Result.Failure(Error.Forbidden("Delivery.UnauthorizedRider", "Rider is not assigned to this delivery."));

        if (Status != DeliveryStatus.Assigned)
            return Result.Failure(Error.Conflict("Delivery.InvalidTransition", $"Cannot mark arrived from status {Status}. Must be Assigned."));

        Status = DeliveryStatus.RiderArrivedAtRestaurant;
        UpdatedAtUtc = now;

        AddDomainEvent(new RiderArrivedAtRestaurantDomainEvent(Id, riderId));
        return Result.Success();
    }

    public Result ConfirmPickup(RiderId riderId, DateTimeOffset now)
    {
        if (RiderId != riderId)
            return Result.Failure(Error.Forbidden("Delivery.UnauthorizedRider", "Rider is not assigned to this delivery."));

        if (Status != DeliveryStatus.RiderArrivedAtRestaurant && Status != DeliveryStatus.Assigned)
            return Result.Failure(Error.Conflict("Delivery.InvalidTransition", $"Cannot confirm pickup from status {Status}."));

        Status = DeliveryStatus.PickedUp;
        PickedUpAtUtc = now;
        UpdatedAtUtc = now;

        AddDomainEvent(new OrderPickedUpDomainEvent(Id, riderId));
        return Result.Success();
    }

    public Result StartDelivery(RiderId riderId, DateTimeOffset now)
    {
        if (RiderId != riderId)
            return Result.Failure(Error.Forbidden("Delivery.UnauthorizedRider", "Rider is not assigned to this delivery."));

        if (Status != DeliveryStatus.PickedUp)
            return Result.Failure(Error.Conflict("Delivery.InvalidTransition", $"Cannot start delivery from status {Status}. Must be PickedUp."));

        Status = DeliveryStatus.OutForDelivery;
        UpdatedAtUtc = now;

        AddDomainEvent(new DeliveryStartedDomainEvent(Id, riderId));
        return Result.Success();
    }

    public Result Complete(RiderId riderId, DateTimeOffset now)
    {
        if (RiderId != riderId)
            return Result.Failure(Error.Forbidden("Delivery.UnauthorizedRider", "Rider is not assigned to this delivery."));

        if (Status != DeliveryStatus.OutForDelivery)
            return Result.Failure(Error.Conflict("Delivery.InvalidTransition", $"Cannot complete delivery from status {Status}. Must be OutForDelivery."));

        Status = DeliveryStatus.Delivered;
        DeliveredAtUtc = now;
        UpdatedAtUtc = now;

        AddDomainEvent(new DeliveryCompletedDomainEvent(Id, riderId));
        return Result.Success();
    }

    public Result Fail(string reason, DateTimeOffset now)
    {
        if (Status == DeliveryStatus.Delivered || Status == DeliveryStatus.Failed || Status == DeliveryStatus.Cancelled)
            return Result.Failure(Error.Conflict("Delivery.TerminalState", $"Cannot fail delivery in terminal state {Status}."));

        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure(Error.Validation("Delivery.EmptyReason", "Failure reason is required."));

        Status = DeliveryStatus.Failed;
        FailedAtUtc = now;
        FailureReason = reason.Trim();
        UpdatedAtUtc = now;

        AddDomainEvent(new DeliveryFailedDomainEvent(Id, FailureReason));
        return Result.Success();
    }
}

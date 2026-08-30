using NextDrop.Modules.Delivery.Domain.Enums;
using NextDrop.Modules.Delivery.Domain.ValueObjects;
using NextDrop.Modules.Orders.Domain.ValueObjects;
using NextDrop.SharedKernel.Common;

namespace NextDrop.Modules.Delivery.Domain.Events;

public record RiderActivatedDomainEvent(RiderId RiderId) : IDomainEvent;

public record RiderAvailabilityChangedDomainEvent(
    RiderId RiderId,
    RiderAvailabilityStatus OldStatus,
    RiderAvailabilityStatus NewStatus) : IDomainEvent;

public record RiderLocationUpdatedDomainEvent(
    RiderId RiderId,
    decimal Latitude,
    decimal Longitude) : IDomainEvent;

public record DeliveryRequestedDomainEvent(
    DeliveryId DeliveryId,
    OrderId OrderId) : IDomainEvent;

public record RiderAssignedDomainEvent(
    DeliveryId DeliveryId,
    RiderId RiderId) : IDomainEvent;

public record DeliveryRejectedDomainEvent(
    DeliveryId DeliveryId,
    RiderId RiderId) : IDomainEvent;

public record RiderArrivedAtRestaurantDomainEvent(
    DeliveryId DeliveryId,
    RiderId RiderId) : IDomainEvent;

public record OrderPickedUpDomainEvent(
    DeliveryId DeliveryId,
    RiderId RiderId) : IDomainEvent;

public record DeliveryStartedDomainEvent(
    DeliveryId DeliveryId,
    RiderId RiderId) : IDomainEvent;

public record DeliveryCompletedDomainEvent(
    DeliveryId DeliveryId,
    RiderId RiderId) : IDomainEvent;

public record DeliveryFailedDomainEvent(
    DeliveryId DeliveryId,
    string Reason) : IDomainEvent;

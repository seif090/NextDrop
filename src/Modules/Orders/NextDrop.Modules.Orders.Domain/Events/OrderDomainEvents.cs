using NextDrop.Modules.Customers.Domain.ValueObjects;
using NextDrop.Modules.Catalog.Domain.ValueObjects;
using NextDrop.Modules.Orders.Domain.Enums;
using NextDrop.Modules.Orders.Domain.ValueObjects;
using NextDrop.Modules.Restaurants.Domain.ValueObjects;
using NextDrop.SharedKernel.Common;

namespace NextDrop.Modules.Orders.Domain.Events;

public record CartCreatedDomainEvent(
    CartId CartId,
    CustomerId CustomerId) : IDomainEvent;

public record CartItemAddedDomainEvent(
    CartId CartId,
    CartItemId CartItemId,
    MenuItemId MenuItemId,
    int Quantity) : IDomainEvent;

public record CartItemRemovedDomainEvent(
    CartId CartId,
    CartItemId CartItemId) : IDomainEvent;

public record OrderCreatedDomainEvent(
    OrderId OrderId,
    string OrderNumber,
    CustomerId CustomerId,
    RestaurantId RestaurantId,
    decimal TotalAmount) : IDomainEvent;

public record OrderConfirmedDomainEvent(
    OrderId OrderId) : IDomainEvent;

public record OrderCancelledDomainEvent(
    OrderId OrderId,
    string Reason) : IDomainEvent;

public record OrderStatusChangedDomainEvent(
    OrderId OrderId,
    OrderStatus OldStatus,
    OrderStatus NewStatus) : IDomainEvent;

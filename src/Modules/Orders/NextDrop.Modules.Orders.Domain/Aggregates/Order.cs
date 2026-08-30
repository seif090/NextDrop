using NextDrop.Modules.Customers.Domain.ValueObjects;
using NextDrop.Modules.Orders.Domain.Entities;
using NextDrop.Modules.Orders.Domain.Enums;
using NextDrop.Modules.Orders.Domain.Events;
using NextDrop.Modules.Orders.Domain.ValueObjects;
using NextDrop.Modules.Restaurants.Domain.ValueObjects;
using NextDrop.SharedKernel.Common;

namespace NextDrop.Modules.Orders.Domain.Aggregates;

public class Order : AggregateRoot<OrderId>
{
    private readonly List<OrderItem> _items = new();

    public string OrderNumber { get; private set; } = string.Empty;
    public CustomerId CustomerId { get; private set; }
    public RestaurantId RestaurantId { get; private set; }
    public RestaurantBranchId RestaurantBranchId { get; private set; }
    public OrderDeliveryAddress DeliveryAddressSnapshot { get; private set; } = null!;
    public string Currency { get; private set; } = "USD";
    public OrderStatus Status { get; private set; }
    public decimal Subtotal { get; private set; }
    public decimal DeliveryFee { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? ConfirmedAtUtc { get; private set; }
    public DateTimeOffset? CancelledAtUtc { get; private set; }
    public string? CancellationReason { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }
    public uint RowVersion { get; private set; }

    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    private Order() { } // EF Core

    private Order(
        OrderId id,
        string orderNumber,
        CustomerId customerId,
        RestaurantId restaurantId,
        RestaurantBranchId branchId,
        OrderDeliveryAddress addressSnapshot,
        string currency,
        decimal deliveryFee,
        decimal minOrderAmount,
        List<OrderItem> items,
        DateTimeOffset now)
        : base(id)
    {
        OrderNumber = orderNumber;
        CustomerId = customerId;
        RestaurantId = restaurantId;
        RestaurantBranchId = branchId;
        DeliveryAddressSnapshot = addressSnapshot;
        Currency = currency;
        Status = OrderStatus.Pending;
        DeliveryFee = Math.Round(deliveryFee, 2, MidpointRounding.AwayFromZero);
        DiscountAmount = 0.00m;
        TaxAmount = 0.00m;
        CreatedAtUtc = now;

        _items.AddRange(items);
        Subtotal = Math.Round(_items.Sum(i => i.LineTotal), 2, MidpointRounding.AwayFromZero);
        TotalAmount = Math.Round(Subtotal + DeliveryFee + TaxAmount - DiscountAmount, 2, MidpointRounding.AwayFromZero);
    }

    public static Result<Order> Create(
        OrderId id,
        string orderNumber,
        CustomerId customerId,
        RestaurantId restaurantId,
        RestaurantBranchId branchId,
        OrderDeliveryAddress addressSnapshot,
        string currency,
        decimal deliveryFee,
        decimal minOrderAmount,
        List<(CartItem CartItem, decimal ServerPrice)> itemsToSnapshot,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(orderNumber))
            return Result.Failure<Order>(Error.Validation("Order.EmptyNumber", "Order number is required."));

        if (customerId == CustomerId.Empty)
            return Result.Failure<Order>(Error.Validation("Order.EmptyCustomer", "Customer ID is required."));

        if (restaurantId == RestaurantId.Empty || branchId == RestaurantBranchId.Empty)
            return Result.Failure<Order>(Error.Validation("Order.EmptyRestaurant", "Restaurant and Branch IDs are required."));

        if (addressSnapshot == null)
            return Result.Failure<Order>(Error.Validation("Order.EmptyAddress", "Delivery address snapshot is required."));

        if (itemsToSnapshot == null || !itemsToSnapshot.Any())
            return Result.Failure<Order>(Error.Validation("Order.EmptyItems", "Cannot create order with no items."));

        var orderItems = itemsToSnapshot.Select(tuple => new OrderItem(
            OrderItemId.New(),
            id,
            tuple.CartItem.MenuItemId,
            tuple.CartItem.VariantId,
            tuple.CartItem.ItemNameSnapshot,
            tuple.CartItem.VariantNameSnapshot,
            tuple.CartItem.Quantity,
            tuple.ServerPrice,
            tuple.CartItem.Notes
        )).ToList();

        var subtotal = Math.Round(orderItems.Sum(i => i.LineTotal), 2, MidpointRounding.AwayFromZero);

        if (minOrderAmount > 0 && subtotal < minOrderAmount)
        {
            return Result.Failure<Order>(Error.Conflict(
                "Order.BelowMinimumAmount",
                $"Order subtotal ({subtotal:C}) is below the restaurant's minimum order requirement ({minOrderAmount:C})."));
        }

        var order = new Order(
            id,
            orderNumber,
            customerId,
            restaurantId,
            branchId,
            addressSnapshot,
            string.IsNullOrWhiteSpace(currency) ? "USD" : currency.Trim().ToUpperInvariant(),
            deliveryFee,
            minOrderAmount,
            orderItems,
            now);

        order.AddDomainEvent(new OrderCreatedDomainEvent(id, orderNumber, customerId, restaurantId, order.TotalAmount));
        return order;
    }

    public Result Confirm(DateTimeOffset now)
    {
        if (Status != OrderStatus.Pending)
            return Result.Failure(Error.Conflict("Order.InvalidTransition", $"Cannot confirm order in status {Status}."));

        Status = OrderStatus.Confirmed;
        ConfirmedAtUtc = now;
        UpdatedAtUtc = now;

        AddDomainEvent(new OrderConfirmedDomainEvent(Id));
        AddDomainEvent(new OrderStatusChangedDomainEvent(Id, OrderStatus.Pending, OrderStatus.Confirmed));
        return Result.Success();
    }

    public Result TransitionTo(OrderStatus newStatus, DateTimeOffset now)
    {
        if (Status == OrderStatus.Delivered || Status == OrderStatus.Cancelled)
            return Result.Failure(Error.Conflict("Order.TerminalState", $"Cannot change status of a terminal order ({Status})."));

        var isAllowed = (Status, newStatus) switch
        {
            (OrderStatus.Pending, OrderStatus.Confirmed) => true,
            (OrderStatus.Pending, OrderStatus.Cancelled) => true,
            (OrderStatus.Confirmed, OrderStatus.Preparing) => true,
            (OrderStatus.Confirmed, OrderStatus.Cancelled) => true,
            (OrderStatus.Preparing, OrderStatus.ReadyForDelivery) => true,
            (OrderStatus.Preparing, OrderStatus.Cancelled) => true,
            (OrderStatus.ReadyForDelivery, OrderStatus.OutForDelivery) => true,
            (OrderStatus.OutForDelivery, OrderStatus.Delivered) => true,
            _ => false
        };

        if (!isAllowed)
            return Result.Failure(Error.Conflict("Order.InvalidTransition", $"Transition from {Status} to {newStatus} is not allowed."));

        var oldStatus = Status;
        Status = newStatus;
        UpdatedAtUtc = now;

        if (newStatus == OrderStatus.Confirmed)
            ConfirmedAtUtc = now;

        AddDomainEvent(new OrderStatusChangedDomainEvent(Id, oldStatus, newStatus));
        return Result.Success();
    }

    public Result Cancel(string reason, DateTimeOffset now)
    {
        if (Status == OrderStatus.Delivered || Status == OrderStatus.Cancelled)
            return Result.Failure(Error.Conflict("Order.TerminalState", $"Cannot cancel order in terminal state ({Status})."));

        if (Status == OrderStatus.ReadyForDelivery || Status == OrderStatus.OutForDelivery)
            return Result.Failure(Error.Conflict("Order.CannotCancelInTransit", $"Cannot cancel order once it is {Status}."));

        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure(Error.Validation("Order.EmptyCancelReason", "Cancellation reason is required."));

        var oldStatus = Status;
        Status = OrderStatus.Cancelled;
        CancelledAtUtc = now;
        CancellationReason = reason.Trim();
        UpdatedAtUtc = now;

        AddDomainEvent(new OrderCancelledDomainEvent(Id, CancellationReason));
        AddDomainEvent(new OrderStatusChangedDomainEvent(Id, oldStatus, OrderStatus.Cancelled));
        return Result.Success();
    }
}

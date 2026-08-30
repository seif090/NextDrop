using FluentAssertions;
using NextDrop.Modules.Catalog.Domain.ValueObjects;
using NextDrop.Modules.Customers.Domain.ValueObjects;
using NextDrop.Modules.Orders.Domain.Aggregates;
using NextDrop.Modules.Orders.Domain.Entities;
using NextDrop.Modules.Orders.Domain.Enums;
using NextDrop.Modules.Orders.Domain.Events;
using NextDrop.Modules.Orders.Domain.ValueObjects;
using NextDrop.Modules.Restaurants.Domain.ValueObjects;
using Xunit;

namespace NextDrop.Domain.Tests;

public class OrdersDomainTests
{
    private readonly CustomerId _customerId = CustomerId.New();
    private readonly RestaurantId _restaurantId = RestaurantId.New();
    private readonly RestaurantBranchId _branchId = RestaurantBranchId.New();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    [Fact]
    public void CreateCart_Should_Succeed_With_Valid_Parameters()
    {
        var cartId = CartId.New();
        var result = Cart.Create(cartId, _customerId, _restaurantId, _branchId, "USD", _now);

        result.IsSuccess.Should().BeTrue();
        result.Value.CustomerId.Should().Be(_customerId);
        result.Value.RestaurantId.Should().Be(_restaurantId);
        result.Value.RestaurantBranchId.Should().Be(_branchId);
        result.Value.DomainEvents.Should().ContainSingle(e => e is CartCreatedDomainEvent);
    }

    [Fact]
    public void AddItem_To_Cart_Should_Enforce_Single_Restaurant_Branch()
    {
        var cart = Cart.Create(CartId.New(), _customerId, _restaurantId, _branchId, "USD", _now).Value;
        var menuItem1 = MenuItemId.New();
        cart.AddItem(CartItemId.New(), _restaurantId, _branchId, menuItem1, null, 2, 10.00m, "Burger", null, null, _now);

        cart.Items.Should().HaveCount(1);

        // Add item from a DIFFERENT restaurant branch
        var newRestId = RestaurantId.New();
        var newBranchId = RestaurantBranchId.New();
        var menuItem2 = MenuItemId.New();

        cart.AddItem(CartItemId.New(), newRestId, newBranchId, menuItem2, null, 1, 15.00m, "Pizza", null, null, _now);

        // Should replace items and update target restaurant branch
        cart.RestaurantId.Should().Be(newRestId);
        cart.RestaurantBranchId.Should().Be(newBranchId);
        cart.Items.Should().HaveCount(1);
        cart.Items.First().MenuItemId.Should().Be(menuItem2);
    }

    [Fact]
    public void AddItem_With_Invalid_Quantity_Should_Fail()
    {
        var cart = Cart.Create(CartId.New(), _customerId, _restaurantId, _branchId, "USD", _now).Value;

        var zeroResult = cart.AddItem(CartItemId.New(), _restaurantId, _branchId, MenuItemId.New(), null, 0, 10.00m, "Item", null, null, _now);
        zeroResult.IsFailure.Should().BeTrue();

        var overMaxResult = cart.AddItem(CartItemId.New(), _restaurantId, _branchId, MenuItemId.New(), null, 51, 10.00m, "Item", null, null, _now);
        overMaxResult.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void CreateOrder_Should_Calculate_Subtotal_And_Total_Correctly()
    {
        var cart = Cart.Create(CartId.New(), _customerId, _restaurantId, _branchId, "USD", _now).Value;
        var cartItem1 = cart.AddItem(CartItemId.New(), _restaurantId, _branchId, MenuItemId.New(), null, 2, 12.50m, "Burger", null, null, _now).Value;
        var cartItem2 = cart.AddItem(CartItemId.New(), _restaurantId, _branchId, MenuItemId.New(), null, 1, 5.00m, "Fries", null, null, _now).Value;

        var itemsToSnapshot = new List<(CartItem CartItem, decimal ServerPrice)>
        {
            (cartItem1, 12.50m), // 2 * 12.50 = 25.00
            (cartItem2, 5.00m)   // 1 * 5.00 = 5.00
        };

        var addressSnapshot = new OrderDeliveryAddress("John", "+123", "Main St", null, "Cairo", "Maadi", "10", "2", "5", 30.0m, 31.0m);

        var orderResult = Order.Create(
            OrderId.New(),
            "ND-2026-TEST0001",
            _customerId,
            _restaurantId,
            _branchId,
            addressSnapshot,
            "USD",
            20.00m, // Delivery fee
            0.00m,  // Min order
            itemsToSnapshot,
            _now);

        orderResult.IsSuccess.Should().BeTrue();
        var order = orderResult.Value;

        order.Subtotal.Should().Be(30.00m);
        order.DeliveryFee.Should().Be(20.00m);
        order.TotalAmount.Should().Be(50.00m);
        order.Status.Should().Be(OrderStatus.Pending);
        order.DomainEvents.Should().ContainSingle(e => e is OrderCreatedDomainEvent);
    }

    [Fact]
    public void CreateOrder_Below_MinimumOrderAmount_Should_Fail()
    {
        var cart = Cart.Create(CartId.New(), _customerId, _restaurantId, _branchId, "USD", _now).Value;
        var cartItem = cart.AddItem(CartItemId.New(), _restaurantId, _branchId, MenuItemId.New(), null, 1, 10.00m, "Fries", null, null, _now).Value;

        var itemsToSnapshot = new List<(CartItem CartItem, decimal ServerPrice)> { (cartItem, 10.00m) };
        var addressSnapshot = new OrderDeliveryAddress("John", "+123", "Main St", null, "Cairo", "Maadi", "10", "2", "5", 30.0m, 31.0m);

        var orderResult = Order.Create(
            OrderId.New(),
            "ND-2026-TEST0002",
            _customerId,
            _restaurantId,
            _branchId,
            addressSnapshot,
            "USD",
            15.00m,
            50.00m, // Min order required = 50.00, subtotal = 10.00
            itemsToSnapshot,
            _now);

        orderResult.IsFailure.Should().BeTrue();
        orderResult.Error.Code.Should().Be("Order.BelowMinimumAmount");
    }

    [Fact]
    public void Order_Status_State_Machine_Transitions_Should_Be_Enforced()
    {
        var cartItem = Cart.Create(CartId.New(), _customerId, _restaurantId, _branchId, "USD", _now).Value
            .AddItem(CartItemId.New(), _restaurantId, _branchId, MenuItemId.New(), null, 1, 20.00m, "Pizza", null, null, _now).Value;

        var itemsToSnapshot = new List<(CartItem CartItem, decimal ServerPrice)> { (cartItem, 20.00m) };
        var addressSnapshot = new OrderDeliveryAddress("John", "+123", "Main St", null, "Cairo", "Maadi", "10", "2", "5", 30.0m, 31.0m);

        var order = Order.Create(OrderId.New(), "ND-2026-TEST0003", _customerId, _restaurantId, _branchId, addressSnapshot, "USD", 10.00m, 0.00m, itemsToSnapshot, _now).Value;

        // Pending -> Confirmed (Valid)
        order.Confirm(_now).IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Confirmed);

        // Confirmed -> Preparing (Valid)
        order.TransitionTo(OrderStatus.Preparing, _now).IsSuccess.Should().BeTrue();

        // Preparing -> ReadyForDelivery (Valid)
        order.TransitionTo(OrderStatus.ReadyForDelivery, _now).IsSuccess.Should().BeTrue();

        // ReadyForDelivery -> OutForDelivery (Valid)
        order.TransitionTo(OrderStatus.OutForDelivery, _now).IsSuccess.Should().BeTrue();

        // OutForDelivery -> Delivered (Valid)
        order.TransitionTo(OrderStatus.Delivered, _now).IsSuccess.Should().BeTrue();

        // Terminal state: Delivered -> Confirmed (Invalid)
        var invalidTransition = order.TransitionTo(OrderStatus.Confirmed, _now);
        invalidTransition.IsFailure.Should().BeTrue();
        invalidTransition.Error.Code.Should().Be("Order.TerminalState");
    }

    [Fact]
    public void CancelOrder_In_Terminal_State_Or_In_Transit_Should_Fail()
    {
        var cartItem = Cart.Create(CartId.New(), _customerId, _restaurantId, _branchId, "USD", _now).Value
            .AddItem(CartItemId.New(), _restaurantId, _branchId, MenuItemId.New(), null, 1, 20.00m, "Pizza", null, null, _now).Value;

        var itemsToSnapshot = new List<(CartItem CartItem, decimal ServerPrice)> { (cartItem, 20.00m) };
        var addressSnapshot = new OrderDeliveryAddress("John", "+123", "Main St", null, "Cairo", "Maadi", "10", "2", "5", 30.0m, 31.0m);

        var order = Order.Create(OrderId.New(), "ND-2026-TEST0004", _customerId, _restaurantId, _branchId, addressSnapshot, "USD", 10.00m, 0.00m, itemsToSnapshot, _now).Value;

        // Transition to OutForDelivery
        order.Confirm(_now);
        order.TransitionTo(OrderStatus.Preparing, _now);
        order.TransitionTo(OrderStatus.ReadyForDelivery, _now);

        // Cancel in transit -> Fail
        var cancelResult = order.Cancel("Customer requested", _now);
        cancelResult.IsFailure.Should().BeTrue();
        cancelResult.Error.Code.Should().Be("Order.CannotCancelInTransit");
    }
}

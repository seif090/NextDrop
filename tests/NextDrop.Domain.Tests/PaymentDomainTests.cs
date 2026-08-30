using FluentAssertions;
using NextDrop.Modules.Catalog.Domain.ValueObjects;
using NextDrop.Modules.Customers.Domain.ValueObjects;
using NextDrop.Modules.Orders.Domain.Aggregates;
using NextDrop.Modules.Orders.Domain.Enums;
using NextDrop.Modules.Orders.Domain.ValueObjects;
using NextDrop.Modules.Payments.Domain.Aggregates;
using NextDrop.Modules.Payments.Domain.Enums;
using NextDrop.Modules.Payments.Domain.ValueObjects;
using NextDrop.Modules.Restaurants.Domain.ValueObjects;
using Xunit;

namespace NextDrop.Domain.Tests;

public class PaymentDomainTests
{
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    [Fact]
    public void Payment_Creation_Should_Set_Status_Pending()
    {
        var paymentId = PaymentId.New();
        var orderId = OrderId.New();
        var userId = Guid.NewGuid();

        var paymentRes = Payment.Create(paymentId, orderId, userId, 100.00m, "USD", PaymentProvider.FakeProvider, _now);

        paymentRes.IsSuccess.Should().BeTrue();
        var payment = paymentRes.Value;
        payment.Status.Should().Be(PaymentStatus.Pending);
        payment.Amount.Should().Be(100.00m);
    }

    [Fact]
    public void Payment_State_Machine_Transitions_Should_Succeed_In_Valid_Sequence()
    {
        var payment = Payment.Create(PaymentId.New(), OrderId.New(), Guid.NewGuid(), 100.00m, "USD", PaymentProvider.FakeProvider, _now).Value;

        payment.Authorize("prov_pay_123", _now).IsSuccess.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Authorized);

        payment.Capture("prov_pay_123", _now).IsSuccess.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Captured);

        payment.MarkRefunded(_now).IsSuccess.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Refunded);
    }

    [Fact]
    public void Captured_Payment_Cannot_Transition_Back_To_Pending_Or_Cancelled()
    {
        var payment = Payment.Create(PaymentId.New(), OrderId.New(), Guid.NewGuid(), 100.00m, "USD", PaymentProvider.FakeProvider, _now).Value;
        payment.Capture("prov_pay_123", _now);

        var cancelRes = payment.Cancel(_now);
        cancelRes.IsFailure.Should().BeTrue();
        cancelRes.Error.Code.Should().Be("Payment.CannotCancelCaptured");
    }

    [Fact]
    public void Refund_Cannot_Exceed_Captured_Payment_Amount()
    {
        var payment = Payment.Create(PaymentId.New(), OrderId.New(), Guid.NewGuid(), 100.00m, "USD", PaymentProvider.FakeProvider, _now).Value;
        payment.Capture("prov_pay_123", _now);

        var overRefundRes = Refund.Create(RefundId.New(), payment, 150.00m, 0.00m, "Customer Complaint", _now);

        overRefundRes.IsFailure.Should().BeTrue();
        overRefundRes.Error.Code.Should().Be("Refund.ExceedsCapturedAmount");
    }

    [Fact]
    public void Cannot_Refund_Non_Captured_Payment()
    {
        var payment = Payment.Create(PaymentId.New(), OrderId.New(), Guid.NewGuid(), 100.00m, "USD", PaymentProvider.FakeProvider, _now).Value;

        var refundRes = Refund.Create(RefundId.New(), payment, 50.00m, 0.00m, "Defective Item", _now);

        refundRes.IsFailure.Should().BeTrue();
        refundRes.Error.Code.Should().Be("Refund.NotCaptured");
    }

    [Fact]
    public void Order_Status_Hardening_Should_Prevent_Illegal_Transitions()
    {
        var orderId = OrderId.New();
        var addressSnapshot = new OrderDeliveryAddress("John", "+123", "Street", null, "City", "District", "1", "2", "3", 30.0m, 31.0m);
        var cartItem = Cart.Create(CartId.New(), CustomerId.New(), RestaurantId.New(), RestaurantBranchId.New(), "USD", _now).Value
            .AddItem(CartItemId.New(), RestaurantId.New(), RestaurantBranchId.New(), MenuItemId.New(), null, 1, 50.00m, "Pizza", null, null, _now).Value;

        var order = Order.Create(orderId, "ND-2026-TST01", CustomerId.New(), RestaurantId.New(), RestaurantBranchId.New(), addressSnapshot, "USD", 10.00m, 0.00m, new List<(NextDrop.Modules.Orders.Domain.Entities.CartItem CartItem, decimal ServerPrice)> { (cartItem, 50.00m) }, _now).Value;

        order.MarkPaid(_now).IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Paid);

        order.TransitionTo(OrderStatus.Preparing, _now).IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Preparing);

        var illegalRes = order.TransitionTo(OrderStatus.PendingPayment, _now);
        illegalRes.IsFailure.Should().BeTrue();
        illegalRes.Error.Code.Should().Be("Order.InvalidTransition");
    }
}

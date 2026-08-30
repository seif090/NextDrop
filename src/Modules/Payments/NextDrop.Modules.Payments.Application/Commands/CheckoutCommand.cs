using FluentValidation;
using MediatR;
using NextDrop.Modules.Catalog.Application.Abstractions;
using NextDrop.Modules.Customers.Application.Abstractions;
using NextDrop.Modules.Customers.Domain.ValueObjects;
using NextDrop.Modules.Orders.Application.Abstractions;
using NextDrop.Modules.Orders.Domain.Aggregates;
using NextDrop.Modules.Orders.Domain.Entities;
using NextDrop.Modules.Orders.Domain.Enums;
using NextDrop.Modules.Orders.Domain.ValueObjects;
using NextDrop.Modules.Payments.Application.Abstractions;
using NextDrop.Modules.Payments.Application.DTOs;
using NextDrop.Modules.Payments.Domain.Aggregates;
using NextDrop.Modules.Payments.Domain.Enums;
using NextDrop.Modules.Payments.Domain.ValueObjects;
using NextDrop.Modules.Restaurants.Application.Abstractions;
using NextDrop.Modules.Restaurants.Domain.Enums;
using NextDrop.SharedKernel.Abstractions;
using NextDrop.SharedKernel.Common;

namespace NextDrop.Modules.Payments.Application.Commands;

public record CheckoutCommand(
    Guid RequesterUserId,
    Guid CartId,
    Guid DeliveryAddressId) : IRequest<Result<TransactionalCheckoutResultDto>>;

public class CheckoutCommandValidator : AbstractValidator<CheckoutCommand>
{
    public CheckoutCommandValidator()
    {
        RuleFor(x => x.CartId).NotEmpty();
        RuleFor(x => x.DeliveryAddressId).NotEmpty();
    }
}

public class CheckoutCommandHandler : IRequestHandler<CheckoutCommand, Result<TransactionalCheckoutResultDto>>
{
    private readonly ICartRepository _cartRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IOrderNumberGenerator _orderNumberGenerator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CheckoutCommandHandler(
        ICartRepository cartRepository,
        ICustomerRepository customerRepository,
        IRestaurantRepository restaurantRepository,
        IMenuItemRepository menuItemRepository,
        IOrderRepository orderRepository,
        IPaymentRepository paymentRepository,
        IOrderNumberGenerator orderNumberGenerator,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _cartRepository = cartRepository;
        _customerRepository = customerRepository;
        _restaurantRepository = restaurantRepository;
        _menuItemRepository = menuItemRepository;
        _orderRepository = orderRepository;
        _paymentRepository = paymentRepository;
        _orderNumberGenerator = orderNumberGenerator;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<TransactionalCheckoutResultDto>> Handle(CheckoutCommand request, CancellationToken cancellationToken)
    {
        var cart = await _cartRepository.GetByIdAsync(new CartId(request.CartId), cancellationToken);
        if (cart == null)
            return Result.Failure<TransactionalCheckoutResultDto>(Error.NotFound("Cart.NotFound", "Cart not found."));

        var customer = await _customerRepository.GetByUserIdAsync(request.RequesterUserId, cancellationToken);
        if (customer == null || cart.CustomerId != customer.Id)
            return Result.Failure<TransactionalCheckoutResultDto>(Error.Forbidden("Cart.Forbidden", "Not authorized to checkout this cart."));

        if (!cart.Items.Any())
            return Result.Failure<TransactionalCheckoutResultDto>(Error.Conflict("Cart.Empty", "Cannot checkout an empty cart."));

        // Validate Delivery Address Ownership
        var address = customer.Addresses.FirstOrDefault(a => a.Id == new CustomerAddressId(request.DeliveryAddressId) && a.IsActive);
        if (address == null)
            return Result.Failure<TransactionalCheckoutResultDto>(Error.Forbidden("Customer.InvalidAddress", "Specified delivery address is invalid or does not belong to customer."));

        // Validate Restaurant & Branch
        var restaurant = await _restaurantRepository.GetByIdAsync(cart.RestaurantId, cancellationToken);
        if (restaurant == null || restaurant.Status != RestaurantStatus.Active)
            return Result.Failure<TransactionalCheckoutResultDto>(Error.Conflict("Restaurant.Unavailable", "Restaurant is currently inactive."));

        var branch = restaurant.Branches.FirstOrDefault(b => b.Id == cart.RestaurantBranchId);
        if (branch == null || branch.Status != BranchStatus.Active)
            return Result.Failure<TransactionalCheckoutResultDto>(Error.Conflict("Restaurant.BranchUnavailable", "Restaurant branch is currently inactive."));

        if (!branch.IsOpenAt(_dateTimeProvider.UtcNow))
            return Result.Failure<TransactionalCheckoutResultDto>(Error.Conflict("Restaurant.Closed", "Restaurant branch is currently closed."));

        var deliveryZone = branch.DeliveryZones.FirstOrDefault(z => z.IsActive);
        var deliveryFee = deliveryZone?.DeliveryFee ?? 0.00m;
        var minOrderAmount = deliveryZone?.MinimumOrderAmount ?? 0.00m;

        // Server-Side Price Resolution from Catalog
        var itemsToSnapshot = new List<(CartItem CartItem, decimal ServerPrice)>();
        foreach (var item in cart.Items)
        {
            var menuItem = await _menuItemRepository.GetByIdAsync(item.MenuItemId, cancellationToken);
            if (menuItem == null || !menuItem.IsAvailable)
                return Result.Failure<TransactionalCheckoutResultDto>(Error.Conflict("Catalog.ItemUnavailable", $"Menu item '{item.ItemNameSnapshot}' is unavailable."));

            itemsToSnapshot.Add((item, menuItem.BasePrice));
        }

        // Create Immutable Order Snapshot
        var orderId = OrderId.New();
        var orderNumber = await _orderNumberGenerator.GenerateOrderNumberAsync(cancellationToken);
        var addressSnapshot = new OrderDeliveryAddress(
            address.RecipientName,
            address.PhoneNumber,
            address.AddressLine1,
            address.AddressLine2,
            address.City,
            address.District,
            address.BuildingNumber,
            address.Floor,
            address.Apartment,
            address.Latitude,
            address.Longitude);

        var orderResult = Order.Create(
            orderId,
            orderNumber,
            customer.Id,
            restaurant.Id,
            branch.Id,
            addressSnapshot,
            cart.Currency,
            deliveryFee,
            minOrderAmount,
            itemsToSnapshot,
            _dateTimeProvider.UtcNow);

        if (orderResult.IsFailure)
            return Result.Failure<TransactionalCheckoutResultDto>(orderResult.Error);

        var order = orderResult.Value;

        // Create Payment Aggregate
        var paymentId = PaymentId.New();
        var paymentResult = Payment.Create(
            paymentId,
            order.Id,
            request.RequesterUserId,
            order.TotalAmount,
            order.Currency,
            PaymentProvider.FakeProvider,
            _dateTimeProvider.UtcNow);

        if (paymentResult.IsFailure)
            return Result.Failure<TransactionalCheckoutResultDto>(paymentResult.Error);

        var payment = paymentResult.Value;

        // Persist Order and Payment, and remove Cart atomically
        await _orderRepository.AddAsync(order, cancellationToken);
        await _paymentRepository.AddAsync(payment, cancellationToken);
        _cartRepository.Delete(cart);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new TransactionalCheckoutResultDto(
            order.Id.Value,
            order.OrderNumber,
            payment.Id.Value,
            order.TotalAmount,
            payment.Status.ToString(),
            order.Status.ToString());
    }
}

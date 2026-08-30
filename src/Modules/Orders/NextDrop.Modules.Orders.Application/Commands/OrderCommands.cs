using FluentValidation;
using MediatR;
using NextDrop.Modules.Catalog.Application.Abstractions;
using NextDrop.Modules.Catalog.Domain.ValueObjects;
using NextDrop.Modules.Customers.Application.Abstractions;
using NextDrop.Modules.Customers.Domain.ValueObjects;
using NextDrop.Modules.Orders.Application.Abstractions;
using NextDrop.Modules.Orders.Application.DTOs;
using NextDrop.Modules.Orders.Domain.Aggregates;
using NextDrop.Modules.Orders.Domain.Entities;
using NextDrop.Modules.Orders.Domain.Enums;
using NextDrop.Modules.Orders.Domain.ValueObjects;
using NextDrop.Modules.Restaurants.Application.Abstractions;
using NextDrop.Modules.Restaurants.Domain.Enums;
using NextDrop.Modules.Restaurants.Domain.ValueObjects;
using NextDrop.SharedKernel.Abstractions;
using NextDrop.SharedKernel.Common;

namespace NextDrop.Modules.Orders.Application.Commands;

// 1. CREATE CART
public record CreateCartCommand(
    Guid RequesterUserId,
    Guid RestaurantId,
    Guid RestaurantBranchId) : IRequest<Result<CartDto>>;

public class CreateCartCommandValidator : AbstractValidator<CreateCartCommand>
{
    public CreateCartCommandValidator()
    {
        RuleFor(x => x.RequesterUserId).NotEmpty();
        RuleFor(x => x.RestaurantId).NotEmpty();
        RuleFor(x => x.RestaurantBranchId).NotEmpty();
    }
}

public class CreateCartCommandHandler : IRequestHandler<CreateCartCommand, Result<CartDto>>
{
    private readonly ICartRepository _cartRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateCartCommandHandler(
        ICartRepository cartRepository,
        ICustomerRepository customerRepository,
        IRestaurantRepository restaurantRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _cartRepository = cartRepository;
        _customerRepository = customerRepository;
        _restaurantRepository = restaurantRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<CartDto>> Handle(CreateCartCommand request, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByUserIdAsync(request.RequesterUserId, cancellationToken);
        if (customer == null)
            return Result.Failure<CartDto>(Error.NotFound("Customer.NotFound", "Customer profile not found."));

        var restId = new RestaurantId(request.RestaurantId);
        var branchId = new RestaurantBranchId(request.RestaurantBranchId);

        var restaurant = await _restaurantRepository.GetByIdAsync(restId, cancellationToken);
        if (restaurant == null || restaurant.Status == RestaurantStatus.Archived || restaurant.Status == RestaurantStatus.Suspended)
            return Result.Failure<CartDto>(Error.Conflict("Restaurant.Unavailable", "Restaurant is currently unavailable."));

        var branch = restaurant.Branches.FirstOrDefault(b => b.Id == branchId && b.Status == BranchStatus.Active);
        if (branch == null)
            return Result.Failure<CartDto>(Error.NotFound("Branch.NotFound", "Restaurant branch not found or inactive."));

        var existingCart = await _cartRepository.GetByCustomerIdAsync(customer.Id, cancellationToken);
        if (existingCart != null)
        {
            var dto = MapToCartDto(existingCart);
            return dto;
        }

        var cartId = CartId.New();
        var cartResult = Cart.Create(cartId, customer.Id, restId, branchId, "USD", _dateTimeProvider.UtcNow);
        if (cartResult.IsFailure)
            return Result.Failure<CartDto>(cartResult.Error);

        await _cartRepository.AddAsync(cartResult.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToCartDto(cartResult.Value);
    }

    internal static CartDto MapToCartDto(Cart cart)
    {
        return new CartDto(
            cart.Id.Value,
            cart.CustomerId.Value,
            cart.RestaurantId.Value,
            cart.RestaurantBranchId.Value,
            cart.Currency,
            cart.Items.Select(i => new CartItemDto(
                i.Id.Value,
                i.MenuItemId.Value,
                i.VariantId?.Value,
                i.Quantity,
                i.UnitPrice,
                i.ItemNameSnapshot,
                i.VariantNameSnapshot,
                i.Notes,
                Math.Round(i.Quantity * i.UnitPrice, 2, MidpointRounding.AwayFromZero)
            )).ToList(),
            cart.CreatedAtUtc);
    }
}

// 2. ADD CART ITEM
public record AddCartItemCommand(
    Guid RequesterUserId,
    Guid CartId,
    Guid MenuItemId,
    Guid? VariantId,
    int Quantity,
    string? Notes) : IRequest<Result<CartDto>>;

public class AddCartItemCommandValidator : AbstractValidator<AddCartItemCommand>
{
    public AddCartItemCommandValidator()
    {
        RuleFor(x => x.RequesterUserId).NotEmpty();
        RuleFor(x => x.CartId).NotEmpty();
        RuleFor(x => x.MenuItemId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0).LessThanOrEqualTo(50);
    }
}

public class AddCartItemCommandHandler : IRequestHandler<AddCartItemCommand, Result<CartDto>>
{
    private readonly ICartRepository _cartRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AddCartItemCommandHandler(
        ICartRepository cartRepository,
        ICustomerRepository customerRepository,
        IMenuItemRepository menuItemRepository,
        IRestaurantRepository restaurantRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _cartRepository = cartRepository;
        _customerRepository = customerRepository;
        _menuItemRepository = menuItemRepository;
        _restaurantRepository = restaurantRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<CartDto>> Handle(AddCartItemCommand request, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByUserIdAsync(request.RequesterUserId, cancellationToken);
        if (customer == null)
            return Result.Failure<CartDto>(Error.NotFound("Customer.NotFound", "Customer profile not found."));

        var cart = await _cartRepository.GetByIdAsync(new CartId(request.CartId), cancellationToken);
        if (cart == null)
            return Result.Failure<CartDto>(Error.NotFound("Cart.NotFound", "Cart not found."));

        if (cart.CustomerId != customer.Id)
            return Result.Failure<CartDto>(Error.Forbidden("Cart.Unauthorized", "Customer does not own this cart."));

        var menuItem = await _menuItemRepository.GetByIdAsync(new MenuItemId(request.MenuItemId), cancellationToken);
        if (menuItem == null || !menuItem.IsActive || !menuItem.IsAvailable)
            return Result.Failure<CartDto>(Error.Conflict("MenuItem.Unavailable", "Menu item is currently unavailable."));

        decimal unitPrice = menuItem.BasePrice;
        string? variantName = null;
        MenuItemVariantId? variantId = null;

        if (request.VariantId.HasValue)
        {
            var variant = menuItem.Variants.FirstOrDefault(v => v.Id == new MenuItemVariantId(request.VariantId.Value) && v.IsActive);
            if (variant == null)
                return Result.Failure<CartDto>(Error.NotFound("Variant.NotFound", "Selected variant not found or inactive."));

            unitPrice = variant.Price;
            variantName = variant.Name;
            variantId = variant.Id;
        }

        var addResult = cart.AddItem(
            CartItemId.New(),
            menuItem.RestaurantId,
            cart.RestaurantBranchId,
            menuItem.Id,
            variantId,
            request.Quantity,
            unitPrice,
            menuItem.Name,
            variantName,
            request.Notes,
            _dateTimeProvider.UtcNow);

        if (addResult.IsFailure)
            return Result.Failure<CartDto>(addResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return CreateCartCommandHandler.MapToCartDto(cart);
    }
}

// 3. CHECKOUT CART
public record CheckoutCartCommand(
    Guid RequesterUserId,
    Guid CartId,
    Guid DeliveryAddressId) : IRequest<Result<CheckoutResultDto>>;

public class CheckoutCartCommandValidator : AbstractValidator<CheckoutCartCommand>
{
    public CheckoutCartCommandValidator()
    {
        RuleFor(x => x.RequesterUserId).NotEmpty();
        RuleFor(x => x.CartId).NotEmpty();
        RuleFor(x => x.DeliveryAddressId).NotEmpty();
    }
}

public class CheckoutCartCommandHandler : IRequestHandler<CheckoutCartCommand, Result<CheckoutResultDto>>
{
    private readonly ICartRepository _cartRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly IOrderNumberGenerator _orderNumberGenerator;
    private readonly ICartCacheService _cacheService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CheckoutCartCommandHandler(
        ICartRepository cartRepository,
        IOrderRepository orderRepository,
        ICustomerRepository customerRepository,
        IRestaurantRepository restaurantRepository,
        IMenuItemRepository menuItemRepository,
        IOrderNumberGenerator orderNumberGenerator,
        ICartCacheService cacheService,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _cartRepository = cartRepository;
        _orderRepository = orderRepository;
        _customerRepository = customerRepository;
        _restaurantRepository = restaurantRepository;
        _menuItemRepository = menuItemRepository;
        _orderNumberGenerator = orderNumberGenerator;
        _cacheService = cacheService;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<CheckoutResultDto>> Handle(CheckoutCartCommand request, CancellationToken cancellationToken)
    {
        // 1. Resolve Customer
        var customer = await _customerRepository.GetByUserIdAsync(request.RequesterUserId, cancellationToken);
        if (customer == null)
            return Result.Failure<CheckoutResultDto>(Error.NotFound("Customer.NotFound", "Customer profile not found."));

        // 2. Resolve Cart
        var cart = await _cartRepository.GetByIdAsync(new CartId(request.CartId), cancellationToken);
        if (cart == null)
            return Result.Failure<CheckoutResultDto>(Error.NotFound("Cart.NotFound", "Cart not found."));

        if (cart.CustomerId != customer.Id)
            return Result.Failure<CheckoutResultDto>(Error.Forbidden("Cart.Unauthorized", "Customer does not own this cart."));

        if (!cart.Items.Any())
            return Result.Failure<CheckoutResultDto>(Error.Conflict("Cart.Empty", "Cannot checkout an empty cart."));

        // 3. Resolve Customer Delivery Address
        var addressEntity = customer.Addresses.FirstOrDefault(a => a.Id == new CustomerAddressId(request.DeliveryAddressId) && a.IsActive);
        if (addressEntity == null)
            return Result.Failure<CheckoutResultDto>(Error.NotFound("Address.NotFound", "Delivery address not found or inactive."));

        // 4. Resolve Restaurant and Branch
        var restaurant = await _restaurantRepository.GetByIdAsync(cart.RestaurantId, cancellationToken);
        if (restaurant == null || restaurant.Status == RestaurantStatus.Archived || restaurant.Status == RestaurantStatus.Suspended)
            return Result.Failure<CheckoutResultDto>(Error.Conflict("Restaurant.Unavailable", "Restaurant is currently unavailable."));

        var branch = restaurant.Branches.FirstOrDefault(b => b.Id == cart.RestaurantBranchId && b.Status == BranchStatus.Active);
        if (branch == null)
            return Result.Failure<CheckoutResultDto>(Error.NotFound("Branch.NotFound", "Restaurant branch not found or inactive."));

        // Verify Branch Operating Hours
        if (!branch.IsOpenAt(_dateTimeProvider.UtcNow))
            return Result.Failure<CheckoutResultDto>(Error.Conflict("Restaurant.Closed", "Restaurant branch is currently closed."));

        // 5. Server-Side Price Resolution and Item Validation
        var itemsToSnapshot = new List<(CartItem CartItem, decimal ServerPrice)>();
        foreach (var cartItem in cart.Items)
        {
            var menuItem = await _menuItemRepository.GetByIdAsync(cartItem.MenuItemId, cancellationToken);
            if (menuItem == null || !menuItem.IsActive || !menuItem.IsAvailable)
                return Result.Failure<CheckoutResultDto>(Error.Conflict("MenuItem.Unavailable", $"Menu item '{cartItem.ItemNameSnapshot}' is currently unavailable."));

            decimal serverPrice = menuItem.BasePrice;
            if (cartItem.VariantId.HasValue)
            {
                var variant = menuItem.Variants.FirstOrDefault(v => v.Id == cartItem.VariantId.Value && v.IsActive);
                if (variant == null)
                    return Result.Failure<CheckoutResultDto>(Error.Conflict("Variant.Unavailable", $"Variant for '{cartItem.ItemNameSnapshot}' is unavailable."));

                serverPrice = variant.Price;
            }

            itemsToSnapshot.Add((cartItem, serverPrice));
        }

        // 6. Delivery Fee and Min Order Amount from Delivery Zone
        decimal deliveryFee = 25.00m; // Default or resolved from zone
        decimal minOrderAmount = 0.00m;

        var deliveryZone = branch.DeliveryZones.FirstOrDefault(z => z.IsActive);
        if (deliveryZone != null)
        {
            deliveryFee = deliveryZone.DeliveryFee;
            minOrderAmount = deliveryZone.MinimumOrderAmount;
        }

        // 7. Create Address Snapshot
        var addressSnapshot = new OrderDeliveryAddress(
            addressEntity.RecipientName,
            addressEntity.PhoneNumber,
            addressEntity.AddressLine1,
            addressEntity.AddressLine2,
            addressEntity.City,
            addressEntity.District,
            addressEntity.BuildingNumber,
            addressEntity.Floor,
            addressEntity.Apartment,
            addressEntity.Latitude,
            addressEntity.Longitude);

        // 8. Generate Unique Order Number
        var orderNumber = await _orderNumberGenerator.GenerateOrderNumberAsync(cancellationToken);
        var orderId = OrderId.New();

        // 9. Create Order Aggregate (validates min order amount server-side!)
        var orderResult = Order.Create(
            orderId,
            orderNumber,
            customer.Id,
            cart.RestaurantId,
            cart.RestaurantBranchId,
            addressSnapshot,
            cart.Currency,
            deliveryFee,
            minOrderAmount,
            itemsToSnapshot,
            _dateTimeProvider.UtcNow);

        if (orderResult.IsFailure)
            return Result.Failure<CheckoutResultDto>(orderResult.Error);

        var order = orderResult.Value;

        // 10. Persist Order and Clear Cart atomically
        await _orderRepository.AddAsync(order, cancellationToken);
        cart.Clear(_dateTimeProvider.UtcNow);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate Redis Cart Cache
        await _cacheService.InvalidateCartAsync(request.RequesterUserId, cancellationToken);

        return new CheckoutResultDto(
            order.Id.Value,
            order.OrderNumber,
            order.Subtotal,
            order.DeliveryFee,
            order.TotalAmount,
            order.Status.ToString());
    }
}

// 4. CANCEL ORDER
public record CancelOrderCommand(
    Guid RequesterUserId,
    Guid OrderId,
    string Reason) : IRequest<Result>;

public class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, Result>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CancelOrderCommandHandler(
        IOrderRepository orderRepository,
        ICustomerRepository customerRepository,
        IRestaurantRepository restaurantRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _orderRepository = orderRepository;
        _customerRepository = customerRepository;
        _restaurantRepository = restaurantRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(new OrderId(request.OrderId), cancellationToken);
        if (order == null)
            return Result.Failure(Error.NotFound("Order.NotFound", "Order not found."));

        // Validate Authorization: Must be Customer owner OR Restaurant staff/owner
        var customer = await _customerRepository.GetByUserIdAsync(request.RequesterUserId, cancellationToken);
        bool isCustomerOwner = customer != null && order.CustomerId == customer.Id;

        var restaurant = await _restaurantRepository.GetByIdAsync(order.RestaurantId, cancellationToken);
        bool isRestaurantStaff = restaurant != null && restaurant.UserHasRole(request.RequesterUserId, RestaurantStaffRole.Owner, RestaurantStaffRole.Manager, RestaurantStaffRole.Staff);

        if (!isCustomerOwner && !isRestaurantStaff)
            return Result.Failure(Error.Forbidden("Order.Unauthorized", "Not authorized to cancel this order."));

        var cancelResult = order.Cancel(request.Reason, _dateTimeProvider.UtcNow);
        if (cancelResult.IsFailure)
            return cancelResult;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

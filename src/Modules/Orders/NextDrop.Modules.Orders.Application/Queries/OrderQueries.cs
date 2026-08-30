using MediatR;
using NextDrop.Modules.Customers.Application.Abstractions;
using NextDrop.Modules.Orders.Application.Abstractions;
using NextDrop.Modules.Orders.Application.DTOs;
using NextDrop.Modules.Orders.Domain.ValueObjects;
using NextDrop.Modules.Restaurants.Application.Abstractions;
using NextDrop.Modules.Restaurants.Domain.Enums;
using NextDrop.Modules.Restaurants.Domain.ValueObjects;
using NextDrop.SharedKernel.Common;

namespace NextDrop.Modules.Orders.Application.Queries;

// 1. GET CART
public record GetCartQuery(Guid RequesterUserId) : IRequest<Result<CartDto>>;

public class GetCartQueryHandler : IRequestHandler<GetCartQuery, Result<CartDto>>
{
    private readonly ICartRepository _cartRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ICartCacheService _cacheService;

    public GetCartQueryHandler(
        ICartRepository cartRepository,
        ICustomerRepository customerRepository,
        ICartCacheService cacheService)
    {
        _cartRepository = cartRepository;
        _customerRepository = customerRepository;
        _cacheService = cacheService;
    }

    public async Task<Result<CartDto>> Handle(GetCartQuery request, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByUserIdAsync(request.RequesterUserId, cancellationToken);
        if (customer == null)
            return Result.Failure<CartDto>(Error.NotFound("Customer.NotFound", "Customer profile not found."));

        var cached = await _cacheService.GetCartAsync(request.RequesterUserId, cancellationToken);
        if (cached != null)
            return cached;

        var cart = await _cartRepository.GetByCustomerIdAsync(customer.Id, cancellationToken);
        if (cart == null)
            return Result.Failure<CartDto>(Error.NotFound("Cart.NotFound", "Active cart not found."));

        var dto = Commands.CreateCartCommandHandler.MapToCartDto(cart);
        await _cacheService.SetCartAsync(request.RequesterUserId, dto, cancellationToken);

        return dto;
    }
}

// 2. GET ORDER BY ID
public record GetOrderByIdQuery(
    Guid RequesterUserId,
    Guid OrderId) : IRequest<Result<OrderDto>>;

public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, Result<OrderDto>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IRestaurantRepository _restaurantRepository;

    public GetOrderByIdQueryHandler(
        IOrderRepository orderRepository,
        ICustomerRepository customerRepository,
        IRestaurantRepository restaurantRepository)
    {
        _orderRepository = orderRepository;
        _customerRepository = customerRepository;
        _restaurantRepository = restaurantRepository;
    }

    public async Task<Result<OrderDto>> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(new OrderId(request.OrderId), cancellationToken);
        if (order == null)
            return Result.Failure<OrderDto>(Error.NotFound("Order.NotFound", "Order not found."));

        // Authorization: Customer owner OR Restaurant staff
        var customer = await _customerRepository.GetByUserIdAsync(request.RequesterUserId, cancellationToken);
        bool isCustomerOwner = customer != null && order.CustomerId == customer.Id;

        var restaurant = await _restaurantRepository.GetByIdAsync(order.RestaurantId, cancellationToken);
        bool isRestaurantStaff = restaurant != null && restaurant.UserHasRole(request.RequesterUserId, RestaurantStaffRole.Owner, RestaurantStaffRole.Manager, RestaurantStaffRole.Staff);

        if (!isCustomerOwner && !isRestaurantStaff)
            return Result.Failure<OrderDto>(Error.Forbidden("Order.Unauthorized", "Not authorized to access this order."));

        return MapToOrderDto(order);
    }

    internal static OrderDto MapToOrderDto(Domain.Aggregates.Order order)
    {
        var addr = order.DeliveryAddressSnapshot;
        var addressDto = new OrderDeliveryAddressDto(
            addr.RecipientName,
            addr.PhoneNumber,
            addr.AddressLine1,
            addr.AddressLine2,
            addr.City,
            addr.District,
            addr.BuildingNumber,
            addr.Floor,
            addr.Apartment,
            addr.Latitude,
            addr.Longitude);

        var itemDtos = order.Items.Select(i => new OrderItemDto(
            i.Id.Value,
            i.MenuItemId.Value,
            i.VariantId?.Value,
            i.ItemName,
            i.VariantName,
            i.Quantity,
            i.UnitPrice,
            i.ModifierSnapshot,
            i.LineTotal
        )).ToList();

        return new OrderDto(
            order.Id.Value,
            order.OrderNumber,
            order.CustomerId.Value,
            order.RestaurantId.Value,
            order.RestaurantBranchId.Value,
            addressDto,
            order.Currency,
            order.Status.ToString(),
            order.Subtotal,
            order.DeliveryFee,
            order.DiscountAmount,
            order.TaxAmount,
            order.TotalAmount,
            itemDtos,
            order.CreatedAtUtc,
            order.ConfirmedAtUtc,
            order.CancelledAtUtc,
            order.CancellationReason);
    }
}

// 3. GET CUSTOMER ORDERS (PAGINATED)
public record GetCustomerOrdersQuery(
    Guid RequesterUserId,
    int Page,
    int PageSize) : IRequest<Result<PagedOrdersDto>>;

public class GetCustomerOrdersQueryHandler : IRequestHandler<GetCustomerOrdersQuery, Result<PagedOrdersDto>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICustomerRepository _customerRepository;

    public GetCustomerOrdersQueryHandler(
        IOrderRepository orderRepository,
        ICustomerRepository customerRepository)
    {
        _orderRepository = orderRepository;
        _customerRepository = customerRepository;
    }

    public async Task<Result<PagedOrdersDto>> Handle(GetCustomerOrdersQuery request, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByUserIdAsync(request.RequesterUserId, cancellationToken);
        if (customer == null)
            return Result.Failure<PagedOrdersDto>(Error.NotFound("Customer.NotFound", "Customer profile not found."));

        int page = Math.Max(1, request.Page);
        int pageSize = Math.Clamp(request.PageSize, 1, 100);

        var (items, totalCount) = await _orderRepository.GetPagedByCustomerIdAsync(customer.Id, page, pageSize, cancellationToken);
        int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var dtos = items.Select(GetOrderByIdQueryHandler.MapToOrderDto).ToList();
        return new PagedOrdersDto(dtos, page, pageSize, totalCount, totalPages);
    }
}

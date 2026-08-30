namespace NextDrop.Modules.Orders.Application.DTOs;

public record CartDto(
    Guid Id,
    Guid CustomerId,
    Guid RestaurantId,
    Guid RestaurantBranchId,
    string Currency,
    List<CartItemDto> Items,
    DateTimeOffset CreatedAtUtc);

public record CartItemDto(
    Guid Id,
    Guid MenuItemId,
    Guid? VariantId,
    int Quantity,
    decimal UnitPrice,
    string ItemName,
    string? VariantName,
    string? Notes,
    decimal LineTotal);

public record OrderDto(
    Guid Id,
    string OrderNumber,
    Guid CustomerId,
    Guid RestaurantId,
    Guid RestaurantBranchId,
    OrderDeliveryAddressDto DeliveryAddress,
    string Currency,
    string Status,
    decimal Subtotal,
    decimal DeliveryFee,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    List<OrderItemDto> Items,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ConfirmedAtUtc,
    DateTimeOffset? CancelledAtUtc,
    string? CancellationReason);

public record OrderItemDto(
    Guid Id,
    Guid MenuItemId,
    Guid? VariantId,
    string ItemName,
    string? VariantName,
    int Quantity,
    decimal UnitPrice,
    string? ModifierSnapshot,
    decimal LineTotal);

public record OrderDeliveryAddressDto(
    string RecipientName,
    string PhoneNumber,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string District,
    string? BuildingNumber,
    string? Floor,
    string? Apartment,
    decimal Latitude,
    decimal Longitude);

public record OrderStatusDto(
    Guid OrderId,
    string OrderNumber,
    string Status,
    DateTimeOffset? UpdatedAtUtc);

public record PagedOrdersDto(
    List<OrderDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public record CheckoutResultDto(
    Guid OrderId,
    string OrderNumber,
    decimal Subtotal,
    decimal DeliveryFee,
    decimal TotalAmount,
    string Status);

using NextDrop.Modules.Catalog.Domain.ValueObjects;
using NextDrop.Modules.Customers.Domain.ValueObjects;
using NextDrop.Modules.Orders.Domain.Entities;
using NextDrop.Modules.Orders.Domain.Events;
using NextDrop.Modules.Orders.Domain.ValueObjects;
using NextDrop.Modules.Restaurants.Domain.ValueObjects;
using NextDrop.SharedKernel.Common;

namespace NextDrop.Modules.Orders.Domain.Aggregates;

public class Cart : AggregateRoot<CartId>
{
    private readonly List<CartItem> _items = new();

    public CustomerId CustomerId { get; private set; }
    public RestaurantId RestaurantId { get; private set; }
    public RestaurantBranchId RestaurantBranchId { get; private set; }
    public string Currency { get; private set; } = "USD";
    public uint RowVersion { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<CartItem> Items => _items.AsReadOnly();

    private Cart() { } // EF Core

    private Cart(
        CartId id,
        CustomerId customerId,
        RestaurantId restaurantId,
        RestaurantBranchId branchId,
        string currency,
        DateTimeOffset now)
        : base(id)
    {
        CustomerId = customerId;
        RestaurantId = restaurantId;
        RestaurantBranchId = branchId;
        Currency = currency;
        CreatedAtUtc = now;
    }

    public static Result<Cart> Create(
        CartId id,
        CustomerId customerId,
        RestaurantId restaurantId,
        RestaurantBranchId branchId,
        string currency,
        DateTimeOffset now)
    {
        if (customerId == CustomerId.Empty)
            return Result.Failure<Cart>(Error.Validation("Cart.EmptyCustomer", "Customer ID is required."));

        if (restaurantId == RestaurantId.Empty || branchId == RestaurantBranchId.Empty)
            return Result.Failure<Cart>(Error.Validation("Cart.EmptyRestaurant", "Restaurant and Branch IDs are required."));

        var cart = new Cart(id, customerId, restaurantId, branchId, string.IsNullOrWhiteSpace(currency) ? "USD" : currency.Trim().ToUpperInvariant(), now);
        cart.AddDomainEvent(new CartCreatedDomainEvent(id, customerId));

        return cart;
    }

    public Result<CartItem> AddItem(
        CartItemId itemId,
        RestaurantId targetRestaurantId,
        RestaurantBranchId targetBranchId,
        MenuItemId menuItemId,
        MenuItemVariantId? variantId,
        int quantity,
        decimal unitPrice,
        string itemName,
        string? variantName,
        string? notes,
        DateTimeOffset now)
    {
        if (quantity <= 0)
            return Result.Failure<CartItem>(Error.Validation("CartItem.InvalidQuantity", "Quantity must be greater than zero."));

        if (quantity > 50)
            return Result.Failure<CartItem>(Error.Validation("CartItem.ExceedsMaxQuantity", "Quantity cannot exceed maximum allowed (50)."));

        if (unitPrice < 0)
            return Result.Failure<CartItem>(Error.Validation("CartItem.NegativePrice", "Unit price cannot be negative."));

        // Single Restaurant Branch Invariant: If item is from another restaurant, clear old items and set new restaurant
        if (RestaurantId != targetRestaurantId || RestaurantBranchId != targetBranchId)
        {
            _items.Clear();
            RestaurantId = targetRestaurantId;
            RestaurantBranchId = targetBranchId;
        }

        var existingItem = _items.FirstOrDefault(i => i.MenuItemId == menuItemId && i.VariantId == variantId);
        if (existingItem != null)
        {
            var updateResult = existingItem.UpdateQuantity(existingItem.Quantity + quantity, now);
            if (updateResult.IsFailure)
                return Result.Failure<CartItem>(updateResult.Error);

            existingItem.UpdatePrice(unitPrice, now);
            UpdatedAtUtc = now;
            return existingItem;
        }

        var newItem = new CartItem(itemId, Id, menuItemId, variantId, quantity, unitPrice, itemName.Trim(), variantName?.Trim(), notes?.Trim(), now);
        _items.Add(newItem);
        UpdatedAtUtc = now;

        AddDomainEvent(new CartItemAddedDomainEvent(Id, itemId, menuItemId, quantity));
        return newItem;
    }

    public Result RemoveItem(CartItemId itemId, DateTimeOffset now)
    {
        var item = _items.FirstOrDefault(i => i.Id == itemId);
        if (item == null)
            return Result.Failure(Error.NotFound("CartItem.NotFound", "Cart item not found."));

        _items.Remove(item);
        UpdatedAtUtc = now;

        AddDomainEvent(new CartItemRemovedDomainEvent(Id, itemId));
        return Result.Success();
    }

    public Result UpdateItemQuantity(CartItemId itemId, int newQuantity, DateTimeOffset now)
    {
        var item = _items.FirstOrDefault(i => i.Id == itemId);
        if (item == null)
            return Result.Failure(Error.NotFound("CartItem.NotFound", "Cart item not found."));

        var updateResult = item.UpdateQuantity(newQuantity, now);
        if (updateResult.IsFailure)
            return updateResult;

        UpdatedAtUtc = now;
        return Result.Success();
    }

    public void Clear(DateTimeOffset now)
    {
        _items.Clear();
        UpdatedAtUtc = now;
    }
}

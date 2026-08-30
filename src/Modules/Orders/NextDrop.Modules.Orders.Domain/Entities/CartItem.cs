using NextDrop.Modules.Catalog.Domain.ValueObjects;
using NextDrop.Modules.Orders.Domain.ValueObjects;
using NextDrop.SharedKernel.Common;

namespace NextDrop.Modules.Orders.Domain.Entities;

public class CartItem : Entity<CartItemId>
{
    public CartId CartId { get; private set; }
    public MenuItemId MenuItemId { get; private set; }
    public MenuItemVariantId? VariantId { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public string ItemNameSnapshot { get; private set; } = string.Empty;
    public string? VariantNameSnapshot { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset AddedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    private CartItem() { } // EF Core

    internal CartItem(
        CartItemId id,
        CartId cartId,
        MenuItemId menuItemId,
        MenuItemVariantId? variantId,
        int quantity,
        decimal unitPrice,
        string itemNameSnapshot,
        string? variantNameSnapshot,
        string? notes,
        DateTimeOffset now)
        : base(id)
    {
        CartId = cartId;
        MenuItemId = menuItemId;
        VariantId = variantId;
        Quantity = quantity;
        UnitPrice = unitPrice;
        ItemNameSnapshot = itemNameSnapshot;
        VariantNameSnapshot = variantNameSnapshot;
        Notes = notes;
        AddedAtUtc = now;
    }

    public Result UpdateQuantity(int newQuantity, DateTimeOffset now)
    {
        if (newQuantity <= 0)
            return Result.Failure(Error.Validation("CartItem.InvalidQuantity", "Quantity must be greater than zero."));

        if (newQuantity > 50)
            return Result.Failure(Error.Validation("CartItem.ExceedsMaxQuantity", "Quantity cannot exceed maximum allowed (50)."));

        Quantity = newQuantity;
        UpdatedAtUtc = now;
        return Result.Success();
    }

    public void UpdatePrice(decimal newPrice, DateTimeOffset now)
    {
        UnitPrice = newPrice;
        UpdatedAtUtc = now;
    }
}

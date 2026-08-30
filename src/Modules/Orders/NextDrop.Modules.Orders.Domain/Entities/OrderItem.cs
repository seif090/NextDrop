using NextDrop.Modules.Catalog.Domain.ValueObjects;
using NextDrop.Modules.Orders.Domain.ValueObjects;
using NextDrop.SharedKernel.Common;

namespace NextDrop.Modules.Orders.Domain.Entities;

public class OrderItem : Entity<OrderItemId>
{
    public OrderId OrderId { get; private set; }
    public MenuItemId MenuItemId { get; private set; }
    public MenuItemVariantId? VariantId { get; private set; }
    public string ItemName { get; private set; } = string.Empty;
    public string? VariantName { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public string? ModifierSnapshot { get; private set; }
    public decimal LineTotal { get; private set; }

    private OrderItem() { } // EF Core

    internal OrderItem(
        OrderItemId id,
        OrderId orderId,
        MenuItemId menuItemId,
        MenuItemVariantId? variantId,
        string itemName,
        string? variantName,
        int quantity,
        decimal unitPrice,
        string? modifierSnapshot)
        : base(id)
    {
        OrderId = orderId;
        MenuItemId = menuItemId;
        VariantId = variantId;
        ItemName = itemName;
        VariantName = variantName;
        Quantity = quantity;
        UnitPrice = unitPrice;
        ModifierSnapshot = modifierSnapshot;
        LineTotal = Math.Round(quantity * unitPrice, 2, MidpointRounding.AwayFromZero);
    }
}

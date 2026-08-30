using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NextDrop.Modules.Catalog.Domain.ValueObjects;
using NextDrop.Modules.Customers.Domain.ValueObjects;
using NextDrop.Modules.Orders.Domain.Aggregates;
using NextDrop.Modules.Orders.Domain.Entities;
using NextDrop.Modules.Orders.Domain.ValueObjects;
using NextDrop.Modules.Restaurants.Domain.ValueObjects;

namespace NextDrop.Infrastructure.Persistence.Configurations;

public class CartConfiguration : IEntityTypeConfiguration<Cart>
{
    public void Configure(EntityTypeBuilder<Cart> builder)
    {
        builder.ToTable("carts", "orders");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasConversion(id => id.Value, value => new CartId(value));

        builder.Property(c => c.CustomerId)
            .HasConversion(id => id.Value, value => new CustomerId(value))
            .IsRequired();

        builder.Property(c => c.RestaurantId)
            .HasConversion(id => id.Value, value => new RestaurantId(value))
            .IsRequired();

        builder.Property(c => c.RestaurantBranchId)
            .HasConversion(id => id.Value, value => new RestaurantBranchId(value))
            .IsRequired();

        builder.Property(c => c.Currency).HasMaxLength(3).IsRequired();

        builder.Property(c => c.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.HasMany(c => c.Items)
            .WithOne()
            .HasForeignKey(ci => ci.CartId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.CustomerId).IsUnique();
        builder.HasIndex(c => new { c.CustomerId, c.RestaurantBranchId });
    }
}

public class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.ToTable("cart_items", "orders");

        builder.HasKey(ci => ci.Id);
        builder.Property(ci => ci.Id)
            .HasConversion(id => id.Value, value => new CartItemId(value));

        builder.Property(ci => ci.CartId)
            .HasConversion(id => id.Value, value => new CartId(value))
            .IsRequired();

        builder.Property(ci => ci.MenuItemId)
            .HasConversion(id => id.Value, value => new MenuItemId(value))
            .IsRequired();

        builder.Property(ci => ci.VariantId)
            .HasConversion(id => id!.Value.Value, value => new MenuItemVariantId(value));

        builder.Property(ci => ci.Quantity).IsRequired();
        builder.Property(ci => ci.UnitPrice).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(ci => ci.ItemNameSnapshot).HasMaxLength(150).IsRequired();
        builder.Property(ci => ci.VariantNameSnapshot).HasMaxLength(100);
        builder.Property(ci => ci.Notes).HasMaxLength(500);

        builder.HasIndex(ci => ci.CartId);
        builder.HasIndex(ci => ci.MenuItemId);
    }
}

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders", "orders");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id)
            .HasConversion(id => id.Value, value => new OrderId(value));

        builder.Property(o => o.OrderNumber).HasMaxLength(32).IsRequired();
        builder.HasIndex(o => o.OrderNumber).IsUnique();

        builder.Property(o => o.CustomerId)
            .HasConversion(id => id.Value, value => new CustomerId(value))
            .IsRequired();

        builder.Property(o => o.RestaurantId)
            .HasConversion(id => id.Value, value => new RestaurantId(value))
            .IsRequired();

        builder.Property(o => o.RestaurantBranchId)
            .HasConversion(id => id.Value, value => new RestaurantBranchId(value))
            .IsRequired();

        builder.Property(o => o.Currency).HasMaxLength(3).IsRequired();
        builder.Property(o => o.Status).HasConversion<int>().IsRequired();

        builder.Property(o => o.Subtotal).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(o => o.DeliveryFee).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(o => o.DiscountAmount).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(o => o.TaxAmount).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(o => o.TotalAmount).HasColumnType("numeric(18,2)").IsRequired();

        builder.Property(o => o.CancellationReason).HasMaxLength(500);

        builder.Property(o => o.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.OwnsOne(o => o.DeliveryAddressSnapshot, addr =>
        {
            addr.ToTable("order_delivery_addresses", "orders");
            addr.Property(a => a.RecipientName).HasMaxLength(150).IsRequired();
            addr.Property(a => a.PhoneNumber).HasMaxLength(50).IsRequired();
            addr.Property(a => a.AddressLine1).HasMaxLength(250).IsRequired();
            addr.Property(a => a.AddressLine2).HasMaxLength(250);
            addr.Property(a => a.City).HasMaxLength(100).IsRequired();
            addr.Property(a => a.District).HasMaxLength(100).IsRequired();
            addr.Property(a => a.BuildingNumber).HasMaxLength(50);
            addr.Property(a => a.Floor).HasMaxLength(50);
            addr.Property(a => a.Apartment).HasMaxLength(50);
            addr.Property(a => a.Latitude).HasColumnType("numeric(9,6)").IsRequired();
            addr.Property(a => a.Longitude).HasColumnType("numeric(9,6)").IsRequired();
        });

        builder.HasMany(o => o.Items)
            .WithOne()
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(o => o.CustomerId);
        builder.HasIndex(o => o.RestaurantId);
        builder.HasIndex(o => o.RestaurantBranchId);
        builder.HasIndex(o => o.Status);
        builder.HasIndex(o => o.CreatedAtUtc);
    }
}

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items", "orders");

        builder.HasKey(oi => oi.Id);
        builder.Property(oi => oi.Id)
            .HasConversion(id => id.Value, value => new OrderItemId(value));

        builder.Property(oi => oi.OrderId)
            .HasConversion(id => id.Value, value => new OrderId(value))
            .IsRequired();

        builder.Property(oi => oi.MenuItemId)
            .HasConversion(id => id.Value, value => new MenuItemId(value))
            .IsRequired();

        builder.Property(oi => oi.VariantId)
            .HasConversion(id => id!.Value.Value, value => new MenuItemVariantId(value));

        builder.Property(oi => oi.ItemName).HasMaxLength(150).IsRequired();
        builder.Property(oi => oi.VariantName).HasMaxLength(100);
        builder.Property(oi => oi.Quantity).IsRequired();
        builder.Property(oi => oi.UnitPrice).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(oi => oi.ModifierSnapshot).HasMaxLength(1000);
        builder.Property(oi => oi.LineTotal).HasColumnType("numeric(18,2)").IsRequired();

        builder.HasIndex(oi => oi.OrderId);
    }
}

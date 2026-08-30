using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NextDrop.Modules.Catalog.Domain.Aggregates;
using NextDrop.Modules.Catalog.Domain.Entities;
using NextDrop.Modules.Catalog.Domain.ValueObjects;
using NextDrop.Modules.Restaurants.Domain.ValueObjects;

namespace NextDrop.Infrastructure.Persistence.Configurations;

public class CatalogConfiguration : IEntityTypeConfiguration<Catalog>
{
    public void Configure(EntityTypeBuilder<Catalog> builder)
    {
        builder.ToTable("catalogs", "catalog");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasConversion(id => id.Value, value => new CatalogId(value));

        builder.Property(c => c.RestaurantId)
            .HasConversion(id => id.Value, value => new RestaurantId(value))
            .IsRequired();

        builder.Property(c => c.Name).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Description).HasMaxLength(500);
        builder.Property(c => c.Status).HasConversion<int>().IsRequired();
        builder.Property(c => c.Version).IsRequired();

        builder.HasMany(c => c.Categories)
            .WithOne()
            .HasForeignKey(cat => cat.CatalogId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.RestaurantId);
    }
}

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories", "catalog");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasConversion(id => id.Value, value => new CategoryId(value));

        builder.Property(c => c.CatalogId)
            .HasConversion(id => id.Value, value => new CatalogId(value))
            .IsRequired();

        builder.Property(c => c.Name).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Description).HasMaxLength(500);
        builder.Property(c => c.DisplayOrder).IsRequired();
        builder.Property(c => c.IsActive).IsRequired();

        builder.HasIndex(c => c.CatalogId);
    }
}

public class MenuItemConfiguration : IEntityTypeConfiguration<MenuItem>
{
    public void Configure(EntityTypeBuilder<MenuItem> builder)
    {
        builder.ToTable("menu_items", "catalog");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id)
            .HasConversion(id => id.Value, value => new MenuItemId(value));

        builder.Property(m => m.CategoryId)
            .HasConversion(id => id.Value, value => new CategoryId(value))
            .IsRequired();

        builder.Property(m => m.RestaurantId)
            .HasConversion(id => id.Value, value => new RestaurantId(value))
            .IsRequired();

        builder.Property(m => m.Name).HasMaxLength(150).IsRequired();
        builder.Property(m => m.Description).HasMaxLength(1000);
        builder.Property(m => m.BasePrice).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(m => m.DisplayOrder).IsRequired();
        builder.Property(m => m.IsAvailable).IsRequired();
        builder.Property(m => m.IsActive).IsRequired();

        // Optimistic Concurrency Token (PostgreSQL xmin)
        builder.Property(m => m.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.HasMany(m => m.Variants)
            .WithOne()
            .HasForeignKey(v => v.MenuItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.ModifierGroups)
            .WithOne()
            .HasForeignKey(mg => mg.MenuItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => m.CategoryId);
        builder.HasIndex(m => m.RestaurantId);
    }
}

public class MenuItemVariantConfiguration : IEntityTypeConfiguration<MenuItemVariant>
{
    public void Configure(EntityTypeBuilder<MenuItemVariant> builder)
    {
        builder.ToTable("menu_item_variants", "catalog");

        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id)
            .HasConversion(id => id.Value, value => new MenuItemVariantId(value));

        builder.Property(v => v.MenuItemId)
            .HasConversion(id => id.Value, value => new MenuItemId(value))
            .IsRequired();

        builder.Property(v => v.Name).HasMaxLength(100).IsRequired();
        builder.Property(v => v.Price).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(v => v.DisplayOrder).IsRequired();
        builder.Property(v => v.IsActive).IsRequired();

        builder.HasIndex(v => v.MenuItemId);
    }
}

public class ModifierGroupConfiguration : IEntityTypeConfiguration<ModifierGroup>
{
    public void Configure(EntityTypeBuilder<ModifierGroup> builder)
    {
        builder.ToTable("modifier_groups", "catalog");

        builder.HasKey(mg => mg.Id);
        builder.Property(mg => mg.Id)
            .HasConversion(id => id.Value, value => new ModifierGroupId(value));

        builder.Property(mg => mg.MenuItemId)
            .HasConversion(id => id.Value, value => new MenuItemId(value))
            .IsRequired();

        builder.Property(mg => mg.Name).HasMaxLength(100).IsRequired();
        builder.Property(mg => mg.MinSelections).IsRequired();
        builder.Property(mg => mg.MaxSelections).IsRequired();
        builder.Property(mg => mg.IsRequired).IsRequired();
        builder.Property(mg => mg.DisplayOrder).IsRequired();
        builder.Property(mg => mg.IsActive).IsRequired();

        builder.HasMany(mg => mg.Options)
            .WithOne()
            .HasForeignKey(o => o.ModifierGroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(mg => mg.MenuItemId);
    }
}

public class ModifierOptionConfiguration : IEntityTypeConfiguration<ModifierOption>
{
    public void Configure(EntityTypeBuilder<ModifierOption> builder)
    {
        builder.ToTable("modifier_options", "catalog");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id)
            .HasConversion(id => id.Value, value => new ModifierOptionId(value));

        builder.Property(o => o.ModifierGroupId)
            .HasConversion(id => id.Value, value => new ModifierGroupId(value))
            .IsRequired();

        builder.Property(o => o.Name).HasMaxLength(100).IsRequired();
        builder.Property(o => o.Price).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(o => o.DisplayOrder).IsRequired();
        builder.Property(o => o.IsActive).IsRequired();

        builder.HasIndex(o => o.ModifierGroupId);
    }
}

public class BranchMenuItemAvailabilityConfiguration : IEntityTypeConfiguration<BranchMenuItemAvailability>
{
    public void Configure(EntityTypeBuilder<BranchMenuItemAvailability> builder)
    {
        builder.ToTable("branch_menu_item_availability", "catalog");

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id)
            .HasConversion(id => id.Value, value => new BranchMenuItemAvailabilityId(value));

        builder.Property(b => b.MenuItemId)
            .HasConversion(id => id.Value, value => new MenuItemId(value))
            .IsRequired();

        builder.Property(b => b.RestaurantBranchId)
            .HasConversion(id => id.Value, value => new RestaurantBranchId(value))
            .IsRequired();

        builder.Property(b => b.RestaurantId)
            .HasConversion(id => id.Value, value => new RestaurantId(value))
            .IsRequired();

        builder.Property(b => b.IsAvailable).IsRequired();

        builder.HasIndex(b => new { b.RestaurantBranchId, b.MenuItemId }).IsUnique();
    }
}

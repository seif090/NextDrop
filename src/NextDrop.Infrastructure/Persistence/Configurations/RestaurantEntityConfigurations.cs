using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NextDrop.Modules.Restaurants.Domain.Aggregates;
using NextDrop.Modules.Restaurants.Domain.Entities;
using NextDrop.Modules.Restaurants.Domain.ValueObjects;

namespace NextDrop.Infrastructure.Persistence.Configurations;

public class RestaurantConfiguration : IEntityTypeConfiguration<Restaurant>
{
    public void Configure(EntityTypeBuilder<Restaurant> builder)
    {
        builder.ToTable("restaurants", "restaurants");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id)
            .HasConversion(id => id.Value, value => RestaurantId.From(value));

        builder.Property(r => r.OwnerUserId).IsRequired();
        builder.HasIndex(r => r.OwnerUserId);

        builder.Property(r => r.Name).HasMaxLength(150).IsRequired();
        builder.Property(r => r.Description).HasMaxLength(500);
        builder.Property(r => r.PhoneNumber).HasMaxLength(20).IsRequired();
        builder.Property(r => r.Email).HasMaxLength(150).IsRequired();
        builder.Property(r => r.Status).HasConversion<int>().IsRequired();

        builder.HasMany(r => r.Branches)
            .WithOne()
            .HasForeignKey(b => b.RestaurantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(r => r.StaffMemberships)
            .WithOne()
            .HasForeignKey(s => s.RestaurantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class RestaurantBranchConfiguration : IEntityTypeConfiguration<RestaurantBranch>
{
    public void Configure(EntityTypeBuilder<RestaurantBranch> builder)
    {
        builder.ToTable("restaurant_branches", "restaurants");

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id)
            .HasConversion(id => id.Value, value => RestaurantBranchId.From(value));

        builder.Property(b => b.RestaurantId)
            .HasConversion(id => id.Value, value => RestaurantId.From(value))
            .IsRequired();

        builder.Property(b => b.Name).HasMaxLength(150).IsRequired();
        builder.Property(b => b.PhoneNumber).HasMaxLength(20).IsRequired();
        builder.Property(b => b.AddressLine1).HasMaxLength(200).IsRequired();
        builder.Property(b => b.City).HasMaxLength(100).IsRequired();
        builder.Property(b => b.District).HasMaxLength(100).IsRequired();
        builder.Property(b => b.Latitude).HasPrecision(9, 6).IsRequired();
        builder.Property(b => b.Longitude).HasPrecision(9, 6).IsRequired();
        builder.Property(b => b.Timezone).HasMaxLength(50).HasDefaultValue("UTC");
        builder.Property(b => b.Status).HasConversion<int>().IsRequired();

        builder.OwnsMany(b => b.OperatingHours, h =>
        {
            h.ToTable("operating_hours", "restaurants");
            h.WithOwner().HasForeignKey("RestaurantBranchId");
            h.Property<Guid>("Id");
            h.HasKey("Id");
            h.Property(x => x.DayOfWeek).HasConversion<int>().IsRequired();
            h.Property(x => x.OpenTime).IsRequired();
            h.Property(x => x.CloseTime).IsRequired();
            h.Property(x => x.IsClosed).IsRequired();
        });

        builder.HasMany(b => b.DeliveryZones)
            .WithOne()
            .HasForeignKey(z => z.BranchId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class RestaurantDeliveryZoneConfiguration : IEntityTypeConfiguration<RestaurantDeliveryZone>
{
    public void Configure(EntityTypeBuilder<RestaurantDeliveryZone> builder)
    {
        builder.ToTable("delivery_zones", "restaurants");

        builder.HasKey(z => z.Id);
        builder.Property(z => z.Id)
            .HasConversion(id => id.Value, value => RestaurantDeliveryZoneId.From(value));

        builder.Property(z => z.BranchId)
            .HasConversion(id => id.Value, value => RestaurantBranchId.From(value))
            .IsRequired();

        builder.Property(z => z.Name).HasMaxLength(100).IsRequired();
        builder.Property(z => z.DeliveryFee).HasPrecision(18, 2).IsRequired();
        builder.Property(z => z.MinimumOrderAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(z => z.EstimatedDeliveryMinutes).IsRequired();
        builder.Property(z => z.IsActive).IsRequired();
    }
}

public class RestaurantStaffMembershipConfiguration : IEntityTypeConfiguration<RestaurantStaffMembership>
{
    public void Configure(EntityTypeBuilder<RestaurantStaffMembership> builder)
    {
        builder.ToTable("staff_memberships", "restaurants");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .HasConversion(id => id.Value, value => RestaurantStaffMembershipId.From(value));

        builder.Property(s => s.RestaurantId)
            .HasConversion(id => id.Value, value => RestaurantId.From(value))
            .IsRequired();

        builder.Property(s => s.UserId).IsRequired();
        builder.Property(s => s.Role).HasConversion<int>().IsRequired();
        builder.Property(s => s.Status).HasConversion<int>().IsRequired();

        builder.HasIndex(s => new { s.RestaurantId, s.UserId });
    }
}

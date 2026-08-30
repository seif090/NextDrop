using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NextDrop.Modules.Customers.Domain.ValueObjects;
using NextDrop.Modules.Delivery.Domain.Aggregates;
using NextDrop.Modules.Delivery.Domain.Enums;
using NextDrop.Modules.Delivery.Domain.ValueObjects;
using NextDrop.Modules.Orders.Domain.ValueObjects;
using NextDrop.Modules.Restaurants.Domain.ValueObjects;
using DeliveryAggregate = NextDrop.Modules.Delivery.Domain.Aggregates.Delivery;

namespace NextDrop.Infrastructure.Persistence.Configurations;

public class RiderEntityConfiguration : IEntityTypeConfiguration<Rider>
{
    public void Configure(EntityTypeBuilder<Rider> builder)
    {
        builder.ToTable("riders", "delivery");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id)
            .HasConversion(id => id.Value, value => new RiderId(value));

        builder.Property(r => r.UserId).IsRequired();
        builder.HasIndex(r => r.UserId).IsUnique();

        builder.Property(r => r.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(r => r.LastName).IsRequired().HasMaxLength(100);
        builder.Property(r => r.PhoneNumber).IsRequired().HasMaxLength(50);

        builder.Property(r => r.Status)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(r => r.AvailabilityStatus)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(30);

        builder.OwnsOne(r => r.Vehicle, v =>
        {
            v.Property(x => x.Type).HasConversion<string>().HasColumnName("vehicle_type").HasMaxLength(30);
            v.Property(x => x.PlateNumber).HasColumnName("vehicle_plate_number").HasMaxLength(50);
            v.Property(x => x.Description).HasColumnName("vehicle_description").HasMaxLength(200);
        });

        builder.OwnsOne(r => r.CurrentLocation, l =>
        {
            l.Property(x => x.Latitude).HasColumnName("latitude").HasColumnType("numeric(18,8)");
            l.Property(x => x.Longitude).HasColumnName("longitude").HasColumnType("numeric(18,8)");
            l.Property(x => x.Accuracy).HasColumnName("accuracy");
            l.Property(x => x.Heading).HasColumnName("heading");
            l.Property(x => x.Speed).HasColumnName("speed");
            l.Property(x => x.RecordedAtUtc).HasColumnName("recorded_at_utc");
        });

        builder.Property(r => r.RowVersion)
            .IsRowVersion();

        builder.Property(r => r.CreatedAtUtc).IsRequired();
        builder.Property(r => r.UpdatedAtUtc).IsRequired();
    }
}

public class DeliveryEntityConfiguration : IEntityTypeConfiguration<DeliveryAggregate>
{
    public void Configure(EntityTypeBuilder<DeliveryAggregate> builder)
    {
        builder.ToTable("deliveries", "delivery");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id)
            .HasConversion(id => id.Value, value => new DeliveryId(value));

        builder.Property(d => d.OrderId)
            .HasConversion(id => id.Value, value => new OrderId(value))
            .IsRequired();

        builder.Property(d => d.BranchId)
            .HasConversion(id => id.Value, value => new RestaurantBranchId(value))
            .IsRequired();

        builder.Property(d => d.CustomerId)
            .HasConversion(id => id.Value, value => new CustomerId(value))
            .IsRequired();

        builder.Property(d => d.RiderId)
            .HasConversion(id => id.HasValue ? id.Value.Value : (Guid?)null, value => value.HasValue ? new RiderId(value.Value) : null);

        builder.Property(d => d.Status)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(d => d.FailureReason).HasMaxLength(500);

        builder.HasIndex(d => d.OrderId);
        builder.HasIndex(d => d.RiderId);
        builder.HasIndex(d => d.BranchId);
        builder.HasIndex(d => d.Status);

        builder.Property(d => d.RowVersion)
            .IsRowVersion();

        builder.Property(d => d.CreatedAtUtc).IsRequired();
        builder.Property(d => d.UpdatedAtUtc).IsRequired();
    }
}

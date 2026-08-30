using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NextDrop.Modules.Customers.Domain.Aggregates;
using NextDrop.Modules.Customers.Domain.Entities;
using NextDrop.Modules.Customers.Domain.ValueObjects;

namespace NextDrop.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers", "customers");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasConversion(id => id.Value, value => CustomerId.From(value));

        builder.Property(c => c.UserId).IsRequired();
        builder.HasIndex(c => c.UserId).IsUnique();

        builder.Property(c => c.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(c => c.LastName).HasMaxLength(100).IsRequired();
        builder.Property(c => c.PhoneNumber).HasMaxLength(20).IsRequired();

        builder.OwnsOne(c => c.Preferences, p =>
        {
            p.Property(x => x.PreferredLanguage).HasColumnName("preferred_language").HasMaxLength(10).HasDefaultValue("en");
            p.Property(x => x.PreferredCurrency).HasColumnName("preferred_currency").HasMaxLength(10).HasDefaultValue("USD");
            p.Property(x => x.AllowMarketingNotifications).HasColumnName("allow_marketing_notifications").HasDefaultValue(true);
            p.Property(x => x.AllowOrderNotifications).HasColumnName("allow_order_notifications").HasDefaultValue(true);
        });

        builder.HasMany(c => c.Addresses)
            .WithOne()
            .HasForeignKey(a => a.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class CustomerAddressConfiguration : IEntityTypeConfiguration<CustomerAddress>
{
    public void Configure(EntityTypeBuilder<CustomerAddress> builder)
    {
        builder.ToTable("customer_addresses", "customers");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .HasConversion(id => id.Value, value => CustomerAddressId.From(value));

        builder.Property(a => a.CustomerId)
            .HasConversion(id => id.Value, value => CustomerId.From(value))
            .IsRequired();

        builder.Property(a => a.Label).HasMaxLength(50).IsRequired();
        builder.Property(a => a.RecipientName).HasMaxLength(100).IsRequired();
        builder.Property(a => a.PhoneNumber).HasMaxLength(20).IsRequired();
        builder.Property(a => a.AddressLine1).HasMaxLength(200).IsRequired();
        builder.Property(a => a.AddressLine2).HasMaxLength(200);
        builder.Property(a => a.City).HasMaxLength(100).IsRequired();
        builder.Property(a => a.District).HasMaxLength(100).IsRequired();

        builder.Property(a => a.Latitude).HasPrecision(9, 6).IsRequired();
        builder.Property(a => a.Longitude).HasPrecision(9, 6).IsRequired();

        // Database protection for single active default address per customer (Directive 3)
        builder.HasIndex(a => a.CustomerId)
            .HasFilter("is_default = true AND is_active = true")
            .IsUnique();
    }
}

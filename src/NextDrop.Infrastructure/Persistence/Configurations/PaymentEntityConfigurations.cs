using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NextDrop.Modules.Orders.Domain.ValueObjects;
using NextDrop.Modules.Payments.Domain.Aggregates;
using NextDrop.Modules.Payments.Domain.Entities;
using NextDrop.Modules.Payments.Domain.ValueObjects;

namespace NextDrop.Infrastructure.Persistence.Configurations;

public class PaymentEntityConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments", "payments");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasConversion(id => id.Value, value => new PaymentId(value));

        builder.Property(p => p.OrderId)
            .HasConversion(id => id.Value, value => new OrderId(value))
            .IsRequired();

        builder.Property(p => p.UserId).IsRequired();

        builder.Property(p => p.Amount)
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(p => p.Currency).IsRequired().HasMaxLength(10);

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(p => p.Provider)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(p => p.ProviderPaymentId).HasMaxLength(100);

        builder.HasIndex(p => p.OrderId);
        builder.HasIndex(p => p.UserId);
        builder.HasIndex(p => p.ProviderPaymentId);
        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => p.CreatedAtUtc);

        builder.OwnsMany(p => p.Transactions, t =>
        {
            t.ToTable("payment_transactions", "payments");

            t.HasKey(x => x.Id);
            t.Property(x => x.Id)
                .HasConversion(id => id.Value, value => new PaymentTransactionId(value));

            t.Property(x => x.PaymentId)
                .HasConversion(id => id.Value, value => new PaymentId(value))
                .IsRequired();

            t.Property(x => x.Type).HasConversion<string>().HasMaxLength(30);
            t.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
            t.Property(x => x.Amount).HasColumnType("numeric(18,2)");
            t.Property(x => x.Currency).HasMaxLength(10);
            t.Property(x => x.ProviderTransactionId).HasMaxLength(100);
            t.Property(x => x.ProviderResponseCode).HasMaxLength(50);
            t.Property(x => x.ProviderReference).HasMaxLength(200);

            t.HasIndex(x => x.PaymentId);
            t.HasIndex(x => x.ProviderTransactionId);
        });

        builder.Property(p => p.RowVersion)
            .IsRowVersion();

        builder.Property(p => p.CreatedAtUtc).IsRequired();
        builder.Property(p => p.UpdatedAtUtc).IsRequired();
    }
}

public class RefundEntityConfiguration : IEntityTypeConfiguration<Refund>
{
    public void Configure(EntityTypeBuilder<Refund> builder)
    {
        builder.ToTable("refunds", "payments");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id)
            .HasConversion(id => id.Value, value => new RefundId(value));

        builder.Property(r => r.PaymentId)
            .HasConversion(id => id.Value, value => new PaymentId(value))
            .IsRequired();

        builder.Property(r => r.OrderId)
            .HasConversion(id => id.Value, value => new OrderId(value))
            .IsRequired();

        builder.Property(r => r.UserId).IsRequired();

        builder.Property(r => r.Amount)
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(r => r.Currency).IsRequired().HasMaxLength(10);

        builder.Property(r => r.Status)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(r => r.Reason).IsRequired().HasMaxLength(300);
        builder.Property(r => r.ProviderRefundId).HasMaxLength(100);

        builder.HasIndex(r => r.PaymentId);
        builder.HasIndex(r => r.OrderId);
        builder.HasIndex(r => r.Status);

        builder.Property(r => r.RowVersion)
            .IsRowVersion();

        builder.Property(r => r.CreatedAtUtc).IsRequired();
    }
}

public class WebhookEventEntityConfiguration : IEntityTypeConfiguration<WebhookEvent>
{
    public void Configure(EntityTypeBuilder<WebhookEvent> builder)
    {
        builder.ToTable("webhook_events", "payments");

        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id)
            .HasConversion(id => id.Value, value => new WebhookEventId(value));

        builder.Property(w => w.Provider).IsRequired().HasMaxLength(50);
        builder.Property(w => w.ProviderEventId).IsRequired().HasMaxLength(100);
        builder.Property(w => w.EventType).IsRequired().HasMaxLength(100);
        builder.Property(w => w.PayloadHash).IsRequired().HasMaxLength(100);

        builder.Property(w => w.ProcessingStatus)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(w => w.FailureReason).HasMaxLength(500);

        builder.HasIndex(w => new { w.Provider, w.ProviderEventId }).IsUnique();
        builder.HasIndex(w => w.ReceivedAtUtc);
    }
}

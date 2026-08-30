using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NextDrop.Modules.Notifications.Domain.Aggregates;
using NextDrop.Modules.Notifications.Domain.Entities;
using NextDrop.Modules.Notifications.Domain.ValueObjects;

namespace NextDrop.Infrastructure.Persistence.Configurations;

public class NotificationEntityConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications", "notifications");

        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id)
            .HasConversion(id => id.Value, value => new NotificationId(value));

        builder.Property(n => n.UserId).IsRequired();
        builder.Property(n => n.Type).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(n => n.Title).IsRequired().HasMaxLength(200);
        builder.Property(n => n.Body).IsRequired().HasMaxLength(1000);
        builder.Property(n => n.DataJson).HasMaxLength(4000);
        builder.Property(n => n.Channel).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(n => n.Priority).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(n => n.Status).HasConversion<string>().HasMaxLength(30).IsRequired();

        builder.HasIndex(n => n.UserId);
        builder.HasIndex(n => n.Status);
        builder.HasIndex(n => n.CreatedAtUtc);
        builder.HasIndex(n => n.ExpiresAtUtc);

        builder.OwnsMany(n => n.Deliveries, d =>
        {
            d.ToTable("notification_deliveries", "notifications");

            d.HasKey(x => x.Id);
            d.Property(x => x.Id)
                .HasConversion(id => id.Value, value => new NotificationDeliveryId(value));

            d.Property(x => x.NotificationId)
                .HasConversion(id => id.Value, value => new NotificationId(value))
                .IsRequired();

            d.Property(x => x.Channel).HasConversion<string>().HasMaxLength(30);
            d.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
            d.Property(x => x.ProviderMessageId).HasMaxLength(100);
            d.Property(x => x.LastError).HasMaxLength(500);

            d.HasIndex(x => x.NotificationId);
            d.HasIndex(x => x.Status);
            d.HasIndex(x => x.NextRetryAtUtc);
        });

        builder.Property(n => n.RowVersion).IsRowVersion();
        builder.Property(n => n.CreatedAtUtc).IsRequired();
    }
}

public class NotificationTemplateEntityConfiguration : IEntityTypeConfiguration<NotificationTemplate>
{
    public void Configure(EntityTypeBuilder<NotificationTemplate> builder)
    {
        builder.ToTable("notification_templates", "notifications");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id)
            .HasConversion(id => id.Value, value => new NotificationTemplateId(value));

        builder.Property(t => t.Type).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(t => t.Channel).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(t => t.Language).IsRequired().HasMaxLength(10);
        builder.Property(t => t.TitleTemplate).IsRequired().HasMaxLength(300);
        builder.Property(t => t.BodyTemplate).IsRequired().HasMaxLength(2000);

        builder.HasIndex(t => new { t.Type, t.Channel, t.Language, t.Version });
    }
}

public class UserNotificationPreferenceEntityConfiguration : IEntityTypeConfiguration<UserNotificationPreference>
{
    public void Configure(EntityTypeBuilder<UserNotificationPreference> builder)
    {
        builder.ToTable("user_preferences", "notifications");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasConversion(id => id.Value, value => new UserNotificationPreferenceId(value));

        builder.Property(p => p.UserId).IsRequired();
        builder.HasIndex(p => p.UserId).IsUnique();
    }
}

public class ProcessedIntegrationEventEntityConfiguration : IEntityTypeConfiguration<ProcessedIntegrationEvent>
{
    public void Configure(EntityTypeBuilder<ProcessedIntegrationEvent> builder)
    {
        builder.ToTable("processed_integration_events", "notifications");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.ConsumerName).IsRequired().HasMaxLength(100);
        builder.Property(e => e.EventId).IsRequired().HasMaxLength(100);

        builder.HasIndex(e => new { e.ConsumerName, e.EventId }).IsUnique();
        builder.HasIndex(e => e.ProcessedAtUtc);
    }
}

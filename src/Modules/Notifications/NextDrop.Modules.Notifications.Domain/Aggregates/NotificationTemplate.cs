using NextDrop.Modules.Notifications.Domain.Enums;
using NextDrop.Modules.Notifications.Domain.ValueObjects;
using NextDrop.SharedKernel.Common;

namespace NextDrop.Modules.Notifications.Domain.Aggregates;

public class NotificationTemplate : AggregateRoot<NotificationTemplateId>
{
    public NotificationType Type { get; private set; }
    public NotificationChannel Channel { get; private set; }
    public string Language { get; private set; } = "en";
    public string TitleTemplate { get; private set; } = string.Empty;
    public string BodyTemplate { get; private set; } = string.Empty;
    public int Version { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private NotificationTemplate() { } // EF Core

    private NotificationTemplate(
        NotificationTemplateId id,
        NotificationType type,
        NotificationChannel channel,
        string language,
        string titleTemplate,
        string bodyTemplate,
        int version,
        DateTimeOffset now)
        : base(id)
    {
        Type = type;
        Channel = channel;
        Language = string.IsNullOrWhiteSpace(language) ? "en" : language.Trim().ToLowerInvariant();
        TitleTemplate = titleTemplate.Trim();
        BodyTemplate = bodyTemplate.Trim();
        Version = version <= 0 ? 1 : version;
        IsActive = true;
        CreatedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public static Result<NotificationTemplate> Create(
        NotificationTemplateId id,
        NotificationType type,
        NotificationChannel channel,
        string language,
        string titleTemplate,
        string bodyTemplate,
        int version,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(titleTemplate))
            return Result.Failure<NotificationTemplate>(Error.Validation("Template.EmptyTitle", "Title template is required."));

        if (string.IsNullOrWhiteSpace(bodyTemplate))
            return Result.Failure<NotificationTemplate>(Error.Validation("Template.EmptyBody", "Body template is required."));

        return new NotificationTemplate(id, type, channel, language, titleTemplate, bodyTemplate, version, now);
    }

    public void Deactivate(DateTimeOffset now)
    {
        IsActive = false;
        UpdatedAtUtc = now;
    }
}

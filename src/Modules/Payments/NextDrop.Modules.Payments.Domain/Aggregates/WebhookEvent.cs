using NextDrop.Modules.Payments.Domain.Enums;
using NextDrop.Modules.Payments.Domain.ValueObjects;
using NextDrop.SharedKernel.Common;

namespace NextDrop.Modules.Payments.Domain.Aggregates;

public class WebhookEvent : AggregateRoot<WebhookEventId>
{
    public string Provider { get; private set; } = string.Empty;
    public string ProviderEventId { get; private set; } = string.Empty;
    public string EventType { get; private set; } = string.Empty;
    public string PayloadHash { get; private set; } = string.Empty;
    public DateTimeOffset ReceivedAtUtc { get; private set; }
    public DateTimeOffset? ProcessedAtUtc { get; private set; }
    public WebhookProcessingStatus ProcessingStatus { get; private set; }
    public string? FailureReason { get; private set; }

    private WebhookEvent() { } // EF Core

    private WebhookEvent(
        WebhookEventId id,
        string provider,
        string providerEventId,
        string eventType,
        string payloadHash,
        DateTimeOffset now)
        : base(id)
    {
        Provider = provider.Trim().ToLowerInvariant();
        ProviderEventId = providerEventId.Trim();
        EventType = eventType.Trim();
        PayloadHash = payloadHash.Trim();
        ProcessingStatus = WebhookProcessingStatus.Pending;
        ReceivedAtUtc = now;
    }

    public static Result<WebhookEvent> Create(
        WebhookEventId id,
        string provider,
        string providerEventId,
        string eventType,
        string payloadHash,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(providerEventId))
            return Result.Failure<WebhookEvent>(Error.Validation("Webhook.EmptyIdentifiers", "Provider and ProviderEventId are required."));

        return new WebhookEvent(id, provider, providerEventId, eventType, payloadHash, now);
    }

    public void MarkProcessed(DateTimeOffset now)
    {
        ProcessingStatus = WebhookProcessingStatus.Processed;
        ProcessedAtUtc = now;
    }

    public void MarkFailed(string reason, DateTimeOffset now)
    {
        ProcessingStatus = WebhookProcessingStatus.Failed;
        FailureReason = reason;
        ProcessedAtUtc = now;
    }

    public void MarkIgnored(string reason, DateTimeOffset now)
    {
        ProcessingStatus = WebhookProcessingStatus.Ignored;
        FailureReason = reason;
        ProcessedAtUtc = now;
    }
}

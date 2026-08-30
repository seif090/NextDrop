using NextDrop.SharedKernel.Common;

namespace NextDrop.Modules.Notifications.Domain.Aggregates;

public class ProcessedIntegrationEvent : AggregateRoot<Guid>
{
    public string ConsumerName { get; private set; } = string.Empty;
    public string EventId { get; private set; } = string.Empty;
    public DateTimeOffset ProcessedAtUtc { get; private set; }

    private ProcessedIntegrationEvent() { } // EF Core

    public ProcessedIntegrationEvent(Guid id, string consumerName, string eventId, DateTimeOffset now)
        : base(id)
    {
        ConsumerName = consumerName.Trim();
        EventId = eventId.Trim();
        ProcessedAtUtc = now;
    }
}

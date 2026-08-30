using System.Text.Json;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NextDrop.Infrastructure.Persistence.Outbox;
using NextDrop.SharedKernel.Common;

namespace NextDrop.Infrastructure.Persistence.Interceptors;

public sealed class DomainEventsToOutboxInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var dbContext = eventData.Context;
        if (dbContext is null)
        {
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        var domainEvents = dbContext.ChangeTracker
            .Entries()
            .Where(e => e.Entity.GetType().GetProperty("DomainEvents") != null)
            .SelectMany(e =>
            {
                var property = e.Entity.GetType().GetProperty("DomainEvents");
                var eventsEnumerable = property?.GetValue(e.Entity) as IEnumerable<IDomainEvent>;
                var eventsList = eventsEnumerable?.ToList() ?? new List<IDomainEvent>();
                
                var clearMethod = e.Entity.GetType().GetMethod("ClearDomainEvents");
                clearMethod?.Invoke(e.Entity, null);

                return eventsList;
            })
            .ToList();

        var outboxMessages = domainEvents.Select(domainEvent => new OutboxMessage
        {
            Id = domainEvent.EventId == Guid.Empty ? Guid.NewGuid() : domainEvent.EventId,
            OccurredOnUtc = domainEvent.OccurredOnUtc,
            Type = domainEvent.GetType().AssemblyQualifiedName ?? domainEvent.GetType().Name,
            Content = JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
            RetryCount = 0
        }).ToList();

        if (outboxMessages.Any())
        {
            dbContext.Set<OutboxMessage>().AddRange(outboxMessages);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}

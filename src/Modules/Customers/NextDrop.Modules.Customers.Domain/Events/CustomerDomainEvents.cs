using NextDrop.SharedKernel.Common;
using NextDrop.Modules.Customers.Domain.ValueObjects;

namespace NextDrop.Modules.Customers.Domain.Events;

public record CustomerCreatedDomainEvent(
    CustomerId CustomerId,
    Guid UserId,
    string FirstName,
    string LastName,
    Guid EventId = default,
    DateTimeOffset OccurredOnUtc = default) : IDomainEvent
{
    public Guid EventId { get; init; } = EventId == default ? Guid.NewGuid() : EventId;
    public DateTimeOffset OccurredOnUtc { get; init; } = OccurredOnUtc == default ? DateTimeOffset.UtcNow : OccurredOnUtc;
}

public record CustomerAddressAddedDomainEvent(
    CustomerId CustomerId,
    CustomerAddressId AddressId,
    bool IsDefault,
    Guid EventId = default,
    DateTimeOffset OccurredOnUtc = default) : IDomainEvent
{
    public Guid EventId { get; init; } = EventId == default ? Guid.NewGuid() : EventId;
    public DateTimeOffset OccurredOnUtc { get; init; } = OccurredOnUtc == default ? DateTimeOffset.UtcNow : OccurredOnUtc;
}

public record CustomerDefaultAddressChangedDomainEvent(
    CustomerId CustomerId,
    CustomerAddressId NewDefaultAddressId,
    Guid EventId = default,
    DateTimeOffset OccurredOnUtc = default) : IDomainEvent
{
    public Guid EventId { get; init; } = EventId == default ? Guid.NewGuid() : EventId;
    public DateTimeOffset OccurredOnUtc { get; init; } = OccurredOnUtc == default ? DateTimeOffset.UtcNow : OccurredOnUtc;
}

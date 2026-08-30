using NextDrop.SharedKernel.Common;
using NextDrop.Modules.Restaurants.Domain.Enums;
using NextDrop.Modules.Restaurants.Domain.ValueObjects;

namespace NextDrop.Modules.Restaurants.Domain.Events;

public record RestaurantCreatedDomainEvent(
    RestaurantId RestaurantId,
    Guid OwnerUserId,
    string Name,
    Guid EventId = default,
    DateTimeOffset OccurredOnUtc = default) : IDomainEvent
{
    public Guid EventId { get; init; } = EventId == default ? Guid.NewGuid() : EventId;
    public DateTimeOffset OccurredOnUtc { get; init; } = OccurredOnUtc == default ? DateTimeOffset.UtcNow : OccurredOnUtc;
}

public record RestaurantActivatedDomainEvent(
    RestaurantId RestaurantId,
    Guid EventId = default,
    DateTimeOffset OccurredOnUtc = default) : IDomainEvent
{
    public Guid EventId { get; init; } = EventId == default ? Guid.NewGuid() : EventId;
    public DateTimeOffset OccurredOnUtc { get; init; } = OccurredOnUtc == default ? DateTimeOffset.UtcNow : OccurredOnUtc;
}

public record RestaurantSuspendedDomainEvent(
    RestaurantId RestaurantId,
    Guid EventId = default,
    DateTimeOffset OccurredOnUtc = default) : IDomainEvent
{
    public Guid EventId { get; init; } = EventId == default ? Guid.NewGuid() : EventId;
    public DateTimeOffset OccurredOnUtc { get; init; } = OccurredOnUtc == default ? DateTimeOffset.UtcNow : OccurredOnUtc;
}

public record RestaurantBranchCreatedDomainEvent(
    RestaurantId RestaurantId,
    RestaurantBranchId BranchId,
    string Name,
    Guid EventId = default,
    DateTimeOffset OccurredOnUtc = default) : IDomainEvent
{
    public Guid EventId { get; init; } = EventId == default ? Guid.NewGuid() : EventId;
    public DateTimeOffset OccurredOnUtc { get; init; } = OccurredOnUtc == default ? DateTimeOffset.UtcNow : OccurredOnUtc;
}

public record RestaurantStaffAddedDomainEvent(
    RestaurantId RestaurantId,
    Guid StaffUserId,
    RestaurantStaffRole Role,
    Guid EventId = default,
    DateTimeOffset OccurredOnUtc = default) : IDomainEvent
{
    public Guid EventId { get; init; } = EventId == default ? Guid.NewGuid() : EventId;
    public DateTimeOffset OccurredOnUtc { get; init; } = OccurredOnUtc == default ? DateTimeOffset.UtcNow : OccurredOnUtc;
}

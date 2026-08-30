using NextDrop.Modules.Delivery.Domain.Enums;
using NextDrop.Modules.Delivery.Domain.Events;
using NextDrop.Modules.Delivery.Domain.ValueObjects;
using NextDrop.SharedKernel.Common;

namespace NextDrop.Modules.Delivery.Domain.Aggregates;

public class Rider : AggregateRoot<RiderId>
{
    public Guid UserId { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string PhoneNumber { get; private set; } = string.Empty;
    public RiderStatus Status { get; private set; }
    public RiderAvailabilityStatus AvailabilityStatus { get; private set; }
    public Vehicle Vehicle { get; private set; } = null!;
    public Location? CurrentLocation { get; private set; }
    public DateTimeOffset? LastLocationUpdatedAt { get; private set; }
    public uint RowVersion { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private Rider() { } // EF Core

    private Rider(
        RiderId id,
        Guid userId,
        string firstName,
        string lastName,
        string phoneNumber,
        Vehicle vehicle,
        DateTimeOffset now)
        : base(id)
    {
        UserId = userId;
        FirstName = firstName;
        LastName = lastName;
        PhoneNumber = phoneNumber;
        Vehicle = vehicle;
        Status = RiderStatus.Pending;
        AvailabilityStatus = RiderAvailabilityStatus.Offline;
        CreatedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public static Result<Rider> Create(
        RiderId id,
        Guid userId,
        string firstName,
        string lastName,
        string phoneNumber,
        Vehicle vehicle,
        DateTimeOffset now)
    {
        if (userId == Guid.Empty)
            return Result.Failure<Rider>(Error.Validation("Rider.EmptyUser", "UserId is required."));

        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            return Result.Failure<Rider>(Error.Validation("Rider.EmptyName", "First and Last Name are required."));

        if (string.IsNullOrWhiteSpace(phoneNumber))
            return Result.Failure<Rider>(Error.Validation("Rider.EmptyPhone", "Phone number is required."));

        if (vehicle == null)
            return Result.Failure<Rider>(Error.Validation("Rider.EmptyVehicle", "Vehicle details are required."));

        return new Rider(id, userId, firstName.Trim(), lastName.Trim(), phoneNumber.Trim(), vehicle, now);
    }

    public Result Activate(DateTimeOffset now)
    {
        if (Status == RiderStatus.Archived || Status == RiderStatus.Blocked)
            return Result.Failure(Error.Conflict("Rider.InvalidTransition", $"Cannot activate rider in status {Status}."));

        Status = RiderStatus.Active;
        UpdatedAtUtc = now;

        AddDomainEvent(new RiderActivatedDomainEvent(Id));
        return Result.Success();
    }

    public Result Suspend(DateTimeOffset now)
    {
        if (Status == RiderStatus.Archived)
            return Result.Failure(Error.Conflict("Rider.TerminalState", "Cannot suspend archived rider."));

        Status = RiderStatus.Suspended;
        AvailabilityStatus = RiderAvailabilityStatus.Offline;
        UpdatedAtUtc = now;
        return Result.Success();
    }

    public Result Block(DateTimeOffset now)
    {
        if (Status == RiderStatus.Archived)
            return Result.Failure(Error.Conflict("Rider.TerminalState", "Cannot block archived rider."));

        Status = RiderStatus.Blocked;
        AvailabilityStatus = RiderAvailabilityStatus.Offline;
        UpdatedAtUtc = now;
        return Result.Success();
    }

    public Result Archive(DateTimeOffset now)
    {
        Status = RiderStatus.Archived;
        AvailabilityStatus = RiderAvailabilityStatus.Offline;
        UpdatedAtUtc = now;
        return Result.Success();
    }

    public Result SetAvailability(RiderAvailabilityStatus newStatus, DateTimeOffset now)
    {
        if (newStatus == RiderAvailabilityStatus.Available && Status != RiderStatus.Active)
            return Result.Failure(Error.Conflict("Rider.NotEligible", $"Rider cannot become Available while status is {Status}. Must be Active."));

        if ((Status == RiderStatus.Blocked || Status == RiderStatus.Archived || Status == RiderStatus.Suspended) && newStatus != RiderAvailabilityStatus.Offline)
            return Result.Failure(Error.Conflict("Rider.NotEligible", $"Ineligible rider ({Status}) must remain Offline."));

        var oldStatus = AvailabilityStatus;
        AvailabilityStatus = newStatus;
        UpdatedAtUtc = now;

        AddDomainEvent(new RiderAvailabilityChangedDomainEvent(Id, oldStatus, newStatus));
        return Result.Success();
    }

    public Result UpdateLocation(Location location, DateTimeOffset now)
    {
        if (Status == RiderStatus.Archived || Status == RiderStatus.Blocked)
            return Result.Failure(Error.Conflict("Rider.IneligibleLocation", $"Rider status {Status} cannot update location."));

        CurrentLocation = location;
        LastLocationUpdatedAt = now;
        UpdatedAtUtc = now;

        AddDomainEvent(new RiderLocationUpdatedDomainEvent(Id, location.Latitude, location.Longitude));
        return Result.Success();
    }
}

using NextDrop.SharedKernel.Common;
using NextDrop.Modules.Restaurants.Domain.Entities;
using NextDrop.Modules.Restaurants.Domain.Enums;
using NextDrop.Modules.Restaurants.Domain.Events;
using NextDrop.Modules.Restaurants.Domain.ValueObjects;

namespace NextDrop.Modules.Restaurants.Domain.Aggregates;

public class Restaurant : AggregateRoot<RestaurantId>
{
    private readonly List<RestaurantBranch> _branches = new();
    private readonly List<RestaurantStaffMembership> _staffMemberships = new();

    public Guid OwnerUserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string PhoneNumber { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public RestaurantStatus Status { get; private set; }
    public IReadOnlyCollection<RestaurantBranch> Branches => _branches.AsReadOnly();
    public IReadOnlyCollection<RestaurantStaffMembership> StaffMemberships => _staffMemberships.AsReadOnly();
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private Restaurant() { } // EF Core

    public static Result<Restaurant> Create(
        RestaurantId id,
        Guid ownerUserId,
        string name,
        string description,
        string phoneNumber,
        string email,
        DateTimeOffset now)
    {
        if (ownerUserId == Guid.Empty)
            return Result.Failure<Restaurant>(Error.Validation("Restaurant.InvalidOwner", "OwnerUserId cannot be empty."));

        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<Restaurant>(Error.Validation("Restaurant.EmptyName", "Restaurant name cannot be empty."));

        if (string.IsNullOrWhiteSpace(phoneNumber))
            return Result.Failure<Restaurant>(Error.Validation("Restaurant.EmptyPhoneNumber", "Phone number cannot be empty."));

        var restaurant = new Restaurant
        {
            Id = id,
            OwnerUserId = ownerUserId,
            Name = name.Trim(),
            Description = description.Trim(),
            PhoneNumber = phoneNumber.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            Status = RestaurantStatus.PendingApproval,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        // Add owner staff membership automatically
        restaurant._staffMemberships.Add(new RestaurantStaffMembership(
            RestaurantStaffMembershipId.New(),
            id,
            ownerUserId,
            RestaurantStaffRole.Owner,
            now));

        restaurant.AddDomainEvent(new RestaurantCreatedDomainEvent(id, ownerUserId, restaurant.Name));
        return Result.Success(restaurant);
    }

    public Result UpdateDetails(string name, string description, string phoneNumber, string email, DateTimeOffset now)
    {
        if (Status == RestaurantStatus.Archived)
            return Result.Failure(Error.Conflict("Restaurant.Archived", "Cannot update details of an archived restaurant."));

        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure(Error.Validation("Restaurant.EmptyName", "Restaurant name cannot be empty."));

        Name = name.Trim();
        Description = description.Trim();
        PhoneNumber = phoneNumber.Trim();
        Email = email.Trim().ToLowerInvariant();
        UpdatedAtUtc = now;

        return Result.Success();
    }

    public Result Activate(DateTimeOffset now)
    {
        if (Status == RestaurantStatus.Archived)
            return Result.Failure(Error.Conflict("Restaurant.InvalidTransition", "Archived restaurant cannot be activated."));

        if (Status == RestaurantStatus.Active)
            return Result.Success();

        Status = RestaurantStatus.Active;
        UpdatedAtUtc = now;
        AddDomainEvent(new RestaurantActivatedDomainEvent(Id));
        return Result.Success();
    }

    public Result CloseTemporarily(DateTimeOffset now)
    {
        if (Status == RestaurantStatus.Archived || Status == RestaurantStatus.Suspended)
            return Result.Failure(Error.Conflict("Restaurant.InvalidTransition", $"Cannot close temporarily from status {Status}."));

        Status = RestaurantStatus.TemporarilyClosed;
        UpdatedAtUtc = now;
        return Result.Success();
    }

    public Result Suspend(DateTimeOffset now)
    {
        if (Status == RestaurantStatus.Archived)
            return Result.Failure(Error.Conflict("Restaurant.InvalidTransition", "Archived restaurant cannot be suspended."));

        Status = RestaurantStatus.Suspended;
        UpdatedAtUtc = now;
        AddDomainEvent(new RestaurantSuspendedDomainEvent(Id));
        return Result.Success();
    }

    public Result Archive(DateTimeOffset now)
    {
        Status = RestaurantStatus.Archived;
        UpdatedAtUtc = now;
        return Result.Success();
    }

    public Result<RestaurantBranch> AddBranch(
        RestaurantBranchId branchId,
        string name,
        string phoneNumber,
        string addressLine1,
        string? addressLine2,
        string city,
        string district,
        decimal latitude,
        decimal longitude,
        string timezone,
        DateTimeOffset now)
    {
        if (Status == RestaurantStatus.Archived)
            return Result.Failure<RestaurantBranch>(Error.Conflict("Restaurant.Archived", "Cannot add branch to an archived restaurant."));

        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<RestaurantBranch>(Error.Validation("Branch.EmptyName", "Branch name cannot be empty."));

        var branch = new RestaurantBranch(
            branchId,
            Id,
            name,
            phoneNumber,
            addressLine1,
            addressLine2,
            city,
            district,
            latitude,
            longitude,
            timezone,
            now);

        _branches.Add(branch);
        UpdatedAtUtc = now;

        AddDomainEvent(new RestaurantBranchCreatedDomainEvent(Id, branchId, branch.Name));
        return Result.Success(branch);
    }

    public Result<RestaurantStaffMembership> AddStaffMember(
        RestaurantStaffMembershipId staffId,
        Guid userId,
        RestaurantStaffRole role,
        DateTimeOffset now)
    {
        if (userId == Guid.Empty)
            return Result.Failure<RestaurantStaffMembership>(Error.Validation("Staff.InvalidUserId", "UserId cannot be empty."));

        var existing = _staffMemberships.FirstOrDefault(s => s.UserId == userId && s.Status == RestaurantStaffStatus.Active);
        if (existing != null)
            return Result.Failure<RestaurantStaffMembership>(Error.Conflict("Staff.AlreadyMember", "User is already an active staff member of this restaurant."));

        var membership = new RestaurantStaffMembership(staffId, Id, userId, role, now);
        _staffMemberships.Add(membership);
        UpdatedAtUtc = now;

        AddDomainEvent(new RestaurantStaffAddedDomainEvent(Id, userId, role));
        return Result.Success(membership);
    }

    public Result UpdateStaffRole(Guid userId, RestaurantStaffRole newRole)
    {
        var membership = _staffMemberships.FirstOrDefault(s => s.UserId == userId && s.Status == RestaurantStaffStatus.Active);
        if (membership == null)
            return Result.Failure(Error.NotFound("Staff.NotFound", "Active staff member not found."));

        membership.UpdateRole(newRole);
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        return Result.Success();
    }

    public Result RemoveStaffMember(Guid userId)
    {
        var membership = _staffMemberships.FirstOrDefault(s => s.UserId == userId && s.Status == RestaurantStaffStatus.Active);
        if (membership == null)
            return Result.Failure(Error.NotFound("Staff.NotFound", "Active staff member not found."));

        if (membership.UserId == OwnerUserId)
            return Result.Failure(Error.Conflict("Staff.CannotRemoveOwner", "Owner staff membership cannot be removed."));

        membership.Deactivate();
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        return Result.Success();
    }

    public bool UserHasRole(Guid userId, params RestaurantStaffRole[] roles)
    {
        if (userId == OwnerUserId)
            return true; // Owner has all permissions

        var membership = _staffMemberships.FirstOrDefault(s => s.UserId == userId && s.Status == RestaurantStaffStatus.Active);
        if (membership == null)
            return false;

        return roles.Contains(membership.Role);
    }
}

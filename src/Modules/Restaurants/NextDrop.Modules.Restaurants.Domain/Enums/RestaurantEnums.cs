namespace NextDrop.Modules.Restaurants.Domain.Enums;

public enum RestaurantStatus
{
    PendingApproval = 1,
    Active = 2,
    TemporarilyClosed = 3,
    Suspended = 4,
    Archived = 5
}

public enum BranchStatus
{
    Active = 1,
    TemporarilyClosed = 2,
    Suspended = 3,
    Archived = 4
}

public enum RestaurantStaffRole
{
    Owner = 1,
    Manager = 2,
    Staff = 3
}

public enum RestaurantStaffStatus
{
    Active = 1,
    Inactive = 2
}

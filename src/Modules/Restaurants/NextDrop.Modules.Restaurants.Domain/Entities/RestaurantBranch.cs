using NextDrop.SharedKernel.Common;
using NextDrop.Modules.Restaurants.Domain.Enums;
using NextDrop.Modules.Restaurants.Domain.ValueObjects;

namespace NextDrop.Modules.Restaurants.Domain.Entities;

public class RestaurantBranch : Entity<RestaurantBranchId>
{
    private readonly List<RestaurantOperatingHours> _operatingHours = new();
    private readonly List<RestaurantDeliveryZone> _deliveryZones = new();

    public RestaurantId RestaurantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string PhoneNumber { get; private set; } = string.Empty;
    public string AddressLine1 { get; private set; } = string.Empty;
    public string? AddressLine2 { get; private set; }
    public string City { get; private set; } = string.Empty;
    public string District { get; private set; } = string.Empty;
    public decimal Latitude { get; private set; }
    public decimal Longitude { get; private set; }
    public string Timezone { get; private set; } = "UTC";
    public BranchStatus Status { get; private set; }
    public IReadOnlyCollection<RestaurantOperatingHours> OperatingHours => _operatingHours.AsReadOnly();
    public IReadOnlyCollection<RestaurantDeliveryZone> DeliveryZones => _deliveryZones.AsReadOnly();
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private RestaurantBranch() { } // EF Core

    public RestaurantBranch(
        RestaurantBranchId id,
        RestaurantId restaurantId,
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
        : base(id)
    {
        RestaurantId = restaurantId;
        Name = name;
        PhoneNumber = phoneNumber;
        AddressLine1 = addressLine1;
        AddressLine2 = addressLine2;
        City = city;
        District = district;
        Latitude = latitude;
        Longitude = longitude;
        Timezone = string.IsNullOrWhiteSpace(timezone) ? "UTC" : timezone;
        Status = BranchStatus.Active;
        CreatedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public void Update(
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
        Name = name;
        PhoneNumber = phoneNumber;
        AddressLine1 = addressLine1;
        AddressLine2 = addressLine2;
        City = city;
        District = district;
        Latitude = latitude;
        Longitude = longitude;
        Timezone = string.IsNullOrWhiteSpace(timezone) ? "UTC" : timezone;
        UpdatedAtUtc = now;
    }

    public void UpdateStatus(BranchStatus status, DateTimeOffset now)
    {
        Status = status;
        UpdatedAtUtc = now;
    }

    public void SetOperatingHours(IEnumerable<RestaurantOperatingHours> hours, DateTimeOffset now)
    {
        _operatingHours.Clear();
        _operatingHours.AddRange(hours);
        UpdatedAtUtc = now;
    }

    public Result<RestaurantDeliveryZone> AddDeliveryZone(
        RestaurantDeliveryZoneId zoneId,
        string name,
        decimal deliveryFee,
        decimal minimumOrderAmount,
        int estimatedDeliveryMinutes,
        DateTimeOffset now)
    {
        if (deliveryFee < 0)
            return Result.Failure<RestaurantDeliveryZone>(Error.Validation("DeliveryZone.InvalidFee", "Delivery fee cannot be negative."));

        if (minimumOrderAmount < 0)
            return Result.Failure<RestaurantDeliveryZone>(Error.Validation("DeliveryZone.InvalidMinimum", "Minimum order amount cannot be negative."));

        if (estimatedDeliveryMinutes <= 0)
            return Result.Failure<RestaurantDeliveryZone>(Error.Validation("DeliveryZone.InvalidDuration", "Estimated delivery minutes must be positive."));

        var zone = new RestaurantDeliveryZone(zoneId, Id, name, deliveryFee, minimumOrderAmount, estimatedDeliveryMinutes, now);
        _deliveryZones.Add(zone);
        UpdatedAtUtc = now;

        return Result.Success(zone);
    }

    public bool IsOpenAt(DateTimeOffset utcTime)
    {
        if (Status != BranchStatus.Active)
            return false;

        TimeZoneInfo tz;
        try
        {
            tz = TimeZoneInfo.FindSystemTimeZoneById(Timezone);
        }
        catch
        {
            tz = TimeZoneInfo.Utc;
        }

        var localTime = TimeZoneInfo.ConvertTime(utcTime, tz);
        var dayOfWeek = localTime.DayOfWeek;
        var timeOnly = TimeOnly.FromDateTime(localTime.DateTime);

        var schedule = _operatingHours.FirstOrDefault(h => h.DayOfWeek == dayOfWeek);
        if (schedule == null)
            return false;

        return schedule.IsOpenAt(timeOnly);
    }
}

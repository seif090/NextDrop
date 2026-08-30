using NextDrop.SharedKernel.Common;

namespace NextDrop.Modules.Restaurants.Domain.ValueObjects;

public class RestaurantOperatingHours : ValueObject
{
    public DayOfWeek DayOfWeek { get; private set; }
    public TimeOnly OpenTime { get; private set; }
    public TimeOnly CloseTime { get; private set; }
    public bool IsClosed { get; private set; }

    private RestaurantOperatingHours() { } // EF Core

    public RestaurantOperatingHours(DayOfWeek dayOfWeek, TimeOnly openTime, TimeOnly closeTime, bool isClosed)
    {
        DayOfWeek = dayOfWeek;
        OpenTime = openTime;
        CloseTime = closeTime;
        IsClosed = isClosed;
    }

    public static RestaurantOperatingHours Closed(DayOfWeek dayOfWeek) =>
        new(dayOfWeek, new TimeOnly(0, 0), new TimeOnly(0, 0), true);

    public static RestaurantOperatingHours Open(DayOfWeek dayOfWeek, TimeOnly openTime, TimeOnly closeTime) =>
        new(dayOfWeek, openTime, closeTime, false);

    public bool IsOpenAt(TimeOnly localTime)
    {
        if (IsClosed)
            return false;

        if (OpenTime == CloseTime)
            return true; // 24 hours open

        if (CloseTime > OpenTime)
        {
            // Standard same-day schedule (e.g., 09:00 to 22:00)
            return localTime >= OpenTime && localTime <= CloseTime;
        }

        // Overnight schedule (e.g., 18:00 to 02:00)
        return localTime >= OpenTime || localTime <= CloseTime;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return DayOfWeek;
        yield return OpenTime;
        yield return CloseTime;
        yield return IsClosed;
    }
}

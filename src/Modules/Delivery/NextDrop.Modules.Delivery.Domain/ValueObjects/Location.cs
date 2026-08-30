using NextDrop.SharedKernel.Common;

namespace NextDrop.Modules.Delivery.Domain.ValueObjects;

public class Location : ValueObject
{
    public decimal Latitude { get; private set; }
    public decimal Longitude { get; private set; }
    public double? Accuracy { get; private set; }
    public double? Heading { get; private set; }
    public double? Speed { get; private set; }
    public DateTimeOffset RecordedAtUtc { get; private set; }

    private Location() { } // EF Core

    public Location(decimal latitude, decimal longitude, double? accuracy, double? heading, double? speed, DateTimeOffset recordedAtUtc)
    {
        Latitude = latitude;
        Longitude = longitude;
        Accuracy = accuracy;
        Heading = heading;
        Speed = speed;
        RecordedAtUtc = recordedAtUtc;
    }

    public static Result<Location> Create(decimal latitude, decimal longitude, double? accuracy, double? heading, double? speed, DateTimeOffset recordedAtUtc)
    {
        if (latitude < -90.0m || latitude > 90.0m)
            return Result.Failure<Location>(Error.Validation("Location.InvalidLatitude", "Latitude must be between -90 and 90 degrees."));

        if (longitude < -180.0m || longitude > 180.0m)
            return Result.Failure<Location>(Error.Validation("Location.InvalidLongitude", "Longitude must be between -180 and 180 degrees."));

        if (double.IsNaN((double)latitude) || double.IsInfinity((double)latitude) ||
            double.IsNaN((double)longitude) || double.IsInfinity((double)longitude))
            return Result.Failure<Location>(Error.Validation("Location.NaNOrInfinity", "Coordinates cannot be NaN or Infinity."));

        return new Location(latitude, longitude, accuracy, heading, speed, recordedAtUtc);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Latitude;
        yield return Longitude;
        yield return Accuracy;
        yield return Heading;
        yield return Speed;
        yield return RecordedAtUtc;
    }
}

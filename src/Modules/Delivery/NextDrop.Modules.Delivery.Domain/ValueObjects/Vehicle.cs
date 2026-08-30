using NextDrop.Modules.Delivery.Domain.Enums;
using NextDrop.SharedKernel.Common;

namespace NextDrop.Modules.Delivery.Domain.ValueObjects;

public class Vehicle : ValueObject
{
    public VehicleType Type { get; private set; }
    public string PlateNumber { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    private Vehicle() { } // EF Core

    public Vehicle(VehicleType type, string plateNumber, string? description)
    {
        Type = type;
        PlateNumber = string.IsNullOrWhiteSpace(plateNumber) ? string.Empty : plateNumber.Trim();
        Description = description?.Trim();
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Type;
        yield return PlateNumber;
        yield return Description;
    }
}

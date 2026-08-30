using NextDrop.SharedKernel.Common;

namespace NextDrop.Modules.Orders.Domain.ValueObjects;

public class OrderDeliveryAddress : ValueObject
{
    public string RecipientName { get; private set; } = string.Empty;
    public string PhoneNumber { get; private set; } = string.Empty;
    public string AddressLine1 { get; private set; } = string.Empty;
    public string? AddressLine2 { get; private set; }
    public string City { get; private set; } = string.Empty;
    public string District { get; private set; } = string.Empty;
    public string? BuildingNumber { get; private set; }
    public string? Floor { get; private set; }
    public string? Apartment { get; private set; }
    public decimal Latitude { get; private set; }
    public decimal Longitude { get; private set; }

    private OrderDeliveryAddress() { } // EF Core

    public OrderDeliveryAddress(
        string recipientName,
        string phoneNumber,
        string addressLine1,
        string? addressLine2,
        string city,
        string district,
        string? buildingNumber,
        string? floor,
        string? apartment,
        decimal latitude,
        decimal longitude)
    {
        RecipientName = recipientName;
        PhoneNumber = phoneNumber;
        AddressLine1 = addressLine1;
        AddressLine2 = addressLine2;
        City = city;
        District = district;
        BuildingNumber = buildingNumber;
        Floor = floor;
        Apartment = apartment;
        Latitude = latitude;
        Longitude = longitude;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return RecipientName;
        yield return PhoneNumber;
        yield return AddressLine1;
        yield return AddressLine2;
        yield return City;
        yield return District;
        yield return BuildingNumber;
        yield return Floor;
        yield return Apartment;
        yield return Latitude;
        yield return Longitude;
    }
}

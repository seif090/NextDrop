using NextDrop.SharedKernel.Common;
using NextDrop.Modules.Customers.Domain.ValueObjects;

namespace NextDrop.Modules.Customers.Domain.Entities;

public class CustomerAddress : Entity<CustomerAddressId>
{
    public CustomerId CustomerId { get; private set; }
    public string Label { get; private set; } = string.Empty;
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
    public bool IsDefault { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private CustomerAddress() { } // EF Core

    public CustomerAddress(
        CustomerAddressId id,
        CustomerId customerId,
        string label,
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
        decimal longitude,
        bool isDefault,
        DateTimeOffset now)
        : base(id)
    {
        CustomerId = customerId;
        Label = label;
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
        IsDefault = isDefault;
        IsActive = true;
        CreatedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public void Update(
        string label,
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
        decimal longitude,
        DateTimeOffset now)
    {
        Label = label;
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
        UpdatedAtUtc = now;
    }

    public void SetDefault(bool isDefault, DateTimeOffset now)
    {
        IsDefault = isDefault;
        UpdatedAtUtc = now;
    }

    public void Deactivate(DateTimeOffset now)
    {
        IsActive = false;
        IsDefault = false;
        UpdatedAtUtc = now;
    }
}

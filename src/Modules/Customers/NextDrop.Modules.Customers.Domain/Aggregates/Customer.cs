using NextDrop.SharedKernel.Common;
using NextDrop.Modules.Customers.Domain.Entities;
using NextDrop.Modules.Customers.Domain.Events;
using NextDrop.Modules.Customers.Domain.ValueObjects;

namespace NextDrop.Modules.Customers.Domain.Aggregates;

public class Customer : AggregateRoot<CustomerId>
{
    private readonly List<CustomerAddress> _addresses = new();

    public Guid UserId { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string PhoneNumber { get; private set; } = string.Empty;
    public CustomerPreferences Preferences { get; private set; } = CustomerPreferences.Default;
    public IReadOnlyCollection<CustomerAddress> Addresses => _addresses.AsReadOnly();
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private Customer() { } // EF Core

    public static Result<Customer> Create(
        CustomerId id,
        Guid userId,
        string firstName,
        string lastName,
        string phoneNumber,
        DateTimeOffset now)
    {
        if (userId == Guid.Empty)
            return Result.Failure<Customer>(Error.Validation("Customer.InvalidUserId", "UserId must not be empty."));

        if (string.IsNullOrWhiteSpace(firstName))
            return Result.Failure<Customer>(Error.Validation("Customer.EmptyFirstName", "First name cannot be empty."));

        if (string.IsNullOrWhiteSpace(lastName))
            return Result.Failure<Customer>(Error.Validation("Customer.EmptyLastName", "Last name cannot be empty."));

        if (string.IsNullOrWhiteSpace(phoneNumber))
            return Result.Failure<Customer>(Error.Validation("Customer.EmptyPhoneNumber", "Phone number cannot be empty."));

        var customer = new Customer
        {
            Id = id,
            UserId = userId,
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            PhoneNumber = phoneNumber.Trim(),
            Preferences = CustomerPreferences.Default,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        customer.AddDomainEvent(new CustomerCreatedDomainEvent(id, userId, customer.FirstName, customer.LastName));
        return Result.Success(customer);
    }

    public Result UpdateProfile(string firstName, string lastName, string phoneNumber, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            return Result.Failure(Error.Validation("Customer.EmptyFirstName", "First name cannot be empty."));

        if (string.IsNullOrWhiteSpace(lastName))
            return Result.Failure(Error.Validation("Customer.EmptyLastName", "Last name cannot be empty."));

        if (string.IsNullOrWhiteSpace(phoneNumber))
            return Result.Failure(Error.Validation("Customer.EmptyPhoneNumber", "Phone number cannot be empty."));

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        PhoneNumber = phoneNumber.Trim();
        UpdatedAtUtc = now;

        return Result.Success();
    }

    public Result<CustomerAddress> AddAddress(
        CustomerAddressId addressId,
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
        bool makeDefault,
        DateTimeOffset now)
    {
        var activeAddresses = _addresses.Where(a => a.IsActive).ToList();
        var shouldBeDefault = makeDefault || !activeAddresses.Any();

        if (shouldBeDefault)
        {
            foreach (var activeAddress in activeAddresses)
            {
                activeAddress.SetDefault(false, now);
            }
        }

        var address = new CustomerAddress(
            addressId,
            Id,
            label,
            recipientName,
            phoneNumber,
            addressLine1,
            addressLine2,
            city,
            district,
            buildingNumber,
            floor,
            apartment,
            latitude,
            longitude,
            shouldBeDefault,
            now);

        _addresses.Add(address);
        UpdatedAtUtc = now;

        AddDomainEvent(new CustomerAddressAddedDomainEvent(Id, addressId, shouldBeDefault));
        return Result.Success(address);
    }

    public Result SetDefaultAddress(CustomerAddressId addressId, DateTimeOffset now)
    {
        var targetAddress = _addresses.FirstOrDefault(a => a.Id == addressId && a.IsActive);
        if (targetAddress == null)
        {
            return Result.Failure(Error.NotFound("CustomerAddress.NotFound", "Active address not found."));
        }

        foreach (var address in _addresses.Where(a => a.IsActive))
        {
            address.SetDefault(address.Id == addressId, now);
        }

        UpdatedAtUtc = now;
        AddDomainEvent(new CustomerDefaultAddressChangedDomainEvent(Id, addressId));
        return Result.Success();
    }

    public Result DeactivateAddress(CustomerAddressId addressId, DateTimeOffset now)
    {
        var targetAddress = _addresses.FirstOrDefault(a => a.Id == addressId && a.IsActive);
        if (targetAddress == null)
        {
            return Result.Failure(Error.NotFound("CustomerAddress.NotFound", "Active address not found."));
        }

        var remainingActive = _addresses.Where(a => a.IsActive && a.Id != addressId).ToList();

        if (targetAddress.IsDefault && remainingActive.Any())
        {
            return Result.Failure(Error.Conflict("CustomerAddress.CannotDeactivateDefault", "Cannot deactivate default address while other active addresses exist. Set another address as default first."));
        }

        targetAddress.Deactivate(now);
        UpdatedAtUtc = now;
        return Result.Success();
    }

    public Result UpdatePreferences(CustomerPreferences preferences, DateTimeOffset now)
    {
        Preferences = preferences;
        UpdatedAtUtc = now;
        return Result.Success();
    }
}

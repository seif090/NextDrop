namespace NextDrop.Modules.Customers.Domain.ValueObjects;

public readonly record struct CustomerAddressId(Guid Value)
{
    public static CustomerAddressId New() => new(Guid.NewGuid());
    public static CustomerAddressId Empty => new(Guid.Empty);
    public static CustomerAddressId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}

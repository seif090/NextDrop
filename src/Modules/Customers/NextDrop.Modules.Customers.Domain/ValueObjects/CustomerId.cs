namespace NextDrop.Modules.Customers.Domain.ValueObjects;

public readonly record struct CustomerId(Guid Value)
{
    public static CustomerId New() => new(Guid.NewGuid());
    public static CustomerId Empty => new(Guid.Empty);
    public static CustomerId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}

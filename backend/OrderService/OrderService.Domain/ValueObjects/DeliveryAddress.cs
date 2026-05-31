namespace OrderService.Domain.ValueObjects;

public sealed record DeliveryAddress
{
    public string AddressLine { get; init; }

    private DeliveryAddress(string addressLine)
    {
        AddressLine = addressLine.Trim();
    }
    public static DeliveryAddress? Create(string? addressLine)
    {
        if (string.IsNullOrWhiteSpace(addressLine))
            return null;

        return new DeliveryAddress(addressLine);
    }

    public static DeliveryAddress CreateRequired(string? addressLine)
    {
        if (string.IsNullOrWhiteSpace(addressLine))
            throw new ArgumentException("Delivery address is required", nameof(addressLine));

        return new DeliveryAddress(addressLine);
    }

    public override string ToString() => AddressLine;
    
    public static implicit operator string?(DeliveryAddress? address) => address?.AddressLine;

    public static explicit operator DeliveryAddress(string value) => CreateRequired(value);

    public void Deconstruct(out string addressLine) => addressLine = AddressLine;
}
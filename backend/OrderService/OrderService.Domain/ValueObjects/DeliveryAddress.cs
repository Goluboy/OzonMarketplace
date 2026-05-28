namespace OrderService.Domain.ValueObjects;

public sealed record DeliveryAddress
{
    public DeliveryAddress(string addressLine)
    {
        if (string.IsNullOrWhiteSpace(addressLine))
        {
            throw new ArgumentException("Address required");
        }

        this.AddressLine = addressLine.Trim();
    }

    public string AddressLine { get; init; }
    public override string ToString() => AddressLine;
    public static implicit operator string(DeliveryAddress deliveryAddress) => deliveryAddress.AddressLine;
    public static explicit operator DeliveryAddress(string value) => new(value);

    public void Deconstruct(out string addressLine)
    {
        addressLine = this.AddressLine;
    }
}

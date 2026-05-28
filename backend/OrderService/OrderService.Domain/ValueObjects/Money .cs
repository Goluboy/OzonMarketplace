namespace OrderService.Domain.ValueObjects;

public sealed record Money
{
    public decimal Value { get; init; }
    public string Currency { get; init; }

    public Money(decimal value, string currency = "RUB")
    {
        if (value < 0)
        {
            throw new ArgumentException("Value must be non-negative", nameof(value));
        }
        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Currency required", nameof(currency));
        }

        this.Value = Value;
        this.Currency = Currency;
    }


    public override string ToString() => $"{Value:0.00} {Currency}";

    public static implicit operator decimal(Money m) => m.Value;
    public static explicit operator Money(decimal value) => new(value);

    public static Money FromString(string s)
    {
        var parts = s.Split(' ');
        return new Money(decimal.Parse(parts[0]), parts.Length > 1 ? parts[1] : "RUB");
    }

    public void Deconstruct(out decimal Value, out string Currency)
    {
        Value = this.Value;
        Currency = this.Currency;
    }
}

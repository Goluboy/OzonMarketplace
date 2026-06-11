namespace ProductService.Domain.ValueObjects;

public record Money
{
    public decimal Amount { get; init; }
    public string Currency { get; init; }
    
    public Money(decimal amount, string currency = "RUB")
    {
        if (amount < 0)
        {
            throw new ArgumentException("Amount cannot be negative.", nameof(amount));
        }
        
        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Currency required", nameof(currency));
        }

        Amount = amount;
        Currency = currency.Trim().ToUpperInvariant();
    }
    
    public override string ToString() => $"{Amount:0.00} {Currency}";

    public static implicit operator decimal(Money m) => m.Amount;
    public static explicit operator Money(decimal value) => new(value);

    public static Money FromString(string s)
    {
        var parts = s.Split(' ');
        return new Money(decimal.Parse(parts[0]), parts.Length > 1 ? parts[1] : "RUB");
    }
}
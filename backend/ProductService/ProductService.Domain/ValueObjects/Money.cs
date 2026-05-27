namespace ProductService.Domain.ValueObjects;

public record Money
{
    public decimal Amount { get; init; }
    
    public Money(decimal amount)
    {
        if (amount < 0)
        {
            throw new ArgumentException("Amount cannot be negative.", nameof(amount));
        }
        
        Amount = amount;
    }
}
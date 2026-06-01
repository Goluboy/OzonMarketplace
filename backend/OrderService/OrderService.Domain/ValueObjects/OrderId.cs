namespace OrderService.Domain.ValueObjects;

public sealed record OrderId(Guid Value)
{
    public static OrderId New() => new(Guid.NewGuid());
    public static implicit operator Guid(OrderId id) => id.Value;
    public static explicit operator OrderId(Guid id) => new(id);
}

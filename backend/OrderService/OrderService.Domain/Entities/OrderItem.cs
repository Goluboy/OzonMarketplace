using OrderService.Domain.Interfaces;

namespace OrderService.Domain.Entities;

public class OrderItem : IAuditable, ICloneable, IEquatable<OrderItem>
{
    public Guid Id { get; init; }
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = null!;
    public int Quantity { get; private set; }
    public decimal PriceAtPurchase { get; private set; }
    public decimal Subtotal { get; private set; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; private set;  }

    private OrderItem() { }
    public override string ToString() =>
        $"OrderItem(Id={Id}, Product={ProductName}, Qty={Quantity}, Subtotal={Subtotal})";

    public static OrderItem Create(Guid productId, string productName, int quantity, decimal price)
    {
        if (quantity <= 0)
        {
            throw new ArgumentException("Quantity must be positive", nameof(quantity));
        }

        if (price < 0)
        {
            throw new ArgumentException("Price must be zero or positive", nameof(price));
        }

        var subtotal = Math.Round(quantity * price, 2, MidpointRounding.AwayFromZero);

        return new OrderItem
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            ProductName = productName,
            Quantity = quantity,
            PriceAtPurchase = price,
            Subtotal = subtotal,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void SetOrderId(Guid orderId)
    {
        if (OrderId != Guid.Empty && OrderId != orderId)
        { 
            throw new InvalidOperationException("Order already set");
        }

        if (OrderId == orderId)
        {
            return;
        }

        OrderId = orderId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateQuantity(int newQuantity)
    {
        if (newQuantity <= 0)
        {
            throw new ArgumentException("Количество должно быть больше нуля", nameof(newQuantity));
        }

        Quantity = newQuantity;
        Subtotal = Math.Round(Quantity * PriceAtPurchase, 2, MidpointRounding.AwayFromZero);
        UpdatedAt = DateTime.UtcNow;
    }

    public object Clone()
    {
        return CloneItem();
    }

    public OrderItem CloneItem()
    {
        return new OrderItem
        {
            Id = Id,
            OrderId = OrderId,
            ProductId = ProductId,
            ProductName = ProductName,
            Quantity = Quantity,
            PriceAtPurchase = PriceAtPurchase,
            Subtotal = Subtotal
        };
    }

    public bool Equals(OrderItem? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Id == other.Id;
    }

    public override bool Equals(object? obj) => Equals(obj as OrderItem);

    public override int GetHashCode() => Id.GetHashCode();

    public static bool operator ==(OrderItem? left, OrderItem? right) => Equals(left, right);
    public static bool operator !=(OrderItem? left, OrderItem? right) => !Equals(left, right);
}
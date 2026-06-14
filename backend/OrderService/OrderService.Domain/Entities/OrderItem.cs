using OrderService.Domain.Interfaces.Domain;
using OrderService.Domain.ValueObjects;

namespace OrderService.Domain.Entities;

public class OrderItem : IAuditable, ICloneable, IEquatable<OrderItem>
{
    public Guid Id { get; init; } = default!;
    public OrderId? OrderId { get; private set; } = default!;
    public Guid ProductId { get; private set; } = default!;
    public string ProductName { get; private set; } = null!;
    public int Quantity { get; private set; } = default!;
    public Money PriceAtPurchase { get; private set; } = default!;
    public Money Subtotal { get; private set; } = default!;
    public DateTime CreatedAt { get; init; } = default!;
    public DateTime? UpdatedAt { get; private set; } = default!;
    public bool IsReserved { get; private set; }
    public int ReservedQuantity { get; private set; }

    private OrderItem() { }

    public static OrderItem Rehydrate(
        Guid id,
        OrderId? orderId,
        Guid productId,
        string productName,
        int quantity,
        Money priceAtPurchase,
        Money subtotal,
        DateTime createdAt,
        DateTime? updatedAt,
        bool isReserved = false,
        int reservedQuantity = 0)
    {
        return new OrderItem
        {
            Id = id,
            OrderId = orderId,
            ProductId = productId,
            ProductName = productName,
            Quantity = quantity,
            PriceAtPurchase = priceAtPurchase,
            Subtotal = subtotal,
            IsReserved = isReserved,
            ReservedQuantity = reservedQuantity,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }

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
            PriceAtPurchase = new Money(price),
            Subtotal = new Money(subtotal),
            IsReserved = false,
            ReservedQuantity = 0,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void SetOrderId(OrderId orderId)
    {
        ArgumentNullException.ThrowIfNull(orderId);

        if (OrderId is not null && OrderId != orderId)
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

    public void MarkAsReserved(int reservedQuantity)
    {
        if (IsReserved)
        {
            throw new InvalidOperationException(
                $"Item '{ProductId}' is already reserved");
        }

        if (reservedQuantity <= 0)
        {
            throw new ArgumentException(
                "Reserved quantity must be positive", nameof(reservedQuantity));
        }

        if (reservedQuantity != Quantity)
        {
            throw new InvalidOperationException(
                $"Reserved quantity ({reservedQuantity}) must match ordered quantity ({Quantity}). " +
                $"Partial reservations are not supported in this SAGA flow.");
        }

        IsReserved = true;
        ReservedQuantity = reservedQuantity;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ReleaseReservation()
    {
        if (!IsReserved)
        {
            return;
        }

        IsReserved = false;
        ReservedQuantity = 0;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateQuantity(int newQuantity)
    {
        if (newQuantity <= 0)
        {
            throw new ArgumentException("Количество должно быть больше нуля", nameof(newQuantity));
        }

        Quantity = newQuantity;
        Subtotal = new Money(Math.Round(Quantity * PriceAtPurchase, 2, MidpointRounding.AwayFromZero));
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
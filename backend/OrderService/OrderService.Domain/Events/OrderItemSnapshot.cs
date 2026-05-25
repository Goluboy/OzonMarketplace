namespace OrderService.Domain.Events;

public record OrderItemSnapshot(
    Guid OrderItemId,
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal PriceAtPurchase,
    decimal Subtotal);
namespace OrderService.Domain.Events;

public record OrderCreatedEvent(
    Guid OrderId,
    Guid CustomerId,
    string CustomerName,
    string CustomerEmail,
    decimal TotalAmount,
    string? DeliveryAddress,
    IReadOnlyCollection<OrderItemSnapshot> Items,
    DateTime CreatedAt
) : DomainEvent;
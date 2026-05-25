namespace OrderService.Domain.Events;

public record OrderItemAddedEvent(
    Guid OrderId,
    OrderItemSnapshot Item,
    decimal NewTotalAmount,
    DateTime ChangedAt
) : DomainEvent;
namespace OrderService.Domain.Events;

public record OrderItemRemovedEvent(
    Guid OrderId,
    Guid OrderItemId,
    Guid ProductId,
    int Quantity,
    decimal RefundedAmount,
    decimal NewTotalAmount,
    DateTime ChangedAt
) : DomainEvent;
namespace OrderService.Domain.Events;

public record OrderCancelledEvent(
    Guid OrderId,
    Guid CustomerId,
    string Reason,
    Guid? CancelledBy,
    DateTime CancelledAt
) : DomainEvent;
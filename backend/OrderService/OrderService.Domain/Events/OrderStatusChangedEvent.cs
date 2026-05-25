using OrderService.Domain.ValueObjects;

namespace OrderService.Domain.Events;

public record OrderStatusChangedEvent(
    Guid OrderId,
    OrderStatus OldStatus,
    OrderStatus NewStatus,
    Guid? ChangedBy,
    string? Comment,
    DateTime ChangedAt
) : DomainEvent;
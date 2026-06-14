using OrderService.Domain.ValueObjects;

namespace OrderService.Domain.Entities;

public class OrderStatusHistory
{
    public Guid Id { get; private set; } = default!;
    public OrderId OrderId { get; private set; } = default!;
    public OrderStatus? OldStatus { get; private set; } = default!;
    public OrderStatus NewStatus { get; private set; } = default!;

    public DateTime ChangedAt { get; private set; } = default!;
    public Guid? ChangedBy { get; private set; } = default!;
    public string? Comment { get; private set; } = default!;

    private OrderStatusHistory() { }

    public static OrderStatusHistory Create(
        Guid orderId,
        OrderStatus oldStatus,
        OrderStatus newStatus,
        Guid? changedBy = null,
        string? comment = null)
    {
        return new OrderStatusHistory
        {
            Id = Guid.NewGuid(),
            OrderId = new OrderId(orderId),
            OldStatus = oldStatus == newStatus ? null : oldStatus,
            NewStatus = newStatus,
            ChangedAt = DateTime.UtcNow,
            ChangedBy = changedBy,
            Comment = comment
        };
    }
}
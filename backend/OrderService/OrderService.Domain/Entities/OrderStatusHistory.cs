using OrderService.Domain.Interfaces;
using OrderService.Domain.ValueObjects;

namespace OrderService.Domain.Entities;

public class OrderStatusHistory
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public OrderStatus? OldStatus { get; private set; }
    public OrderStatus NewStatus { get; private set; }

    public DateTime ChangedAt { get; private set; }
    public Guid? ChangedBy { get; private set; }
    public string? Comment { get; private set; }

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
            OrderId = orderId,
            OldStatus = oldStatus == newStatus ? null : oldStatus,
            NewStatus = newStatus,
            ChangedAt = DateTime.UtcNow,
            ChangedBy = changedBy,
            Comment = comment
        };
    }
}
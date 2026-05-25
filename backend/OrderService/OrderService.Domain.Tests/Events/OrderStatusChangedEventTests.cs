using FluentAssertions;
using OrderService.Domain.Events;
using OrderService.Domain.ValueObjects;

namespace OrderService.Domain.Tests.Events;

public class OrderStatusChangedEventTests
{
    [Fact]
    public void OrderStatusChangedEvent_ShouldInitializeAllProperties()
    {
        var orderId = Guid.NewGuid();
        const OrderStatus oldStatus = OrderStatus.Created;
        const OrderStatus newStatus = OrderStatus.Paid;
        var changedBy = Guid.NewGuid();
        const string comment = "Payment received";
        var changedAt = DateTime.UtcNow;

        var orderEvent = new OrderStatusChangedEvent(
            orderId,
            oldStatus,
            newStatus,
            changedBy,
            comment,
            changedAt);

        orderEvent.OrderId.Should().Be(orderId);
        orderEvent.OldStatus.Should().Be(oldStatus);
        orderEvent.NewStatus.Should().Be(newStatus);
        orderEvent.ChangedBy.Should().Be(changedBy);
        orderEvent.Comment.Should().Be(comment);
        orderEvent.ChangedAt.Should().Be(changedAt);
    }

    [Fact]
    public void OrderStatusChangedEvent_WithNullChangedBy_ShouldSucceed()
    {
        var orderId = Guid.NewGuid();

        var orderEvent = new OrderStatusChangedEvent(
            orderId,
            OrderStatus.Created,
            OrderStatus.Paid,
            null,
            null,
            DateTime.UtcNow);

        orderEvent.ChangedBy.Should().BeNull();
        orderEvent.Comment.Should().BeNull();
    }
}
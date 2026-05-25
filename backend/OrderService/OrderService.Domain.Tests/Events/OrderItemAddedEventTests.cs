using FluentAssertions;
using OrderService.Domain.Events;

namespace OrderService.Domain.Tests.Events;

public class OrderItemAddedEventTests
{
    [Fact]
    public void OrderItemAddedEvent_ShouldInitializeAllProperties()
    {
        var orderId = Guid.NewGuid();
        var itemSnapshot = new OrderItemSnapshot(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test Product",
            3,
            15.00m,
            45.00m);
        const decimal newTotalAmount = 150.00m;
        var changedAt = DateTime.UtcNow;

        var orderEvent = new OrderItemAddedEvent(
            orderId,
            itemSnapshot,
            newTotalAmount,
            changedAt);

        orderEvent.OrderId.Should().Be(orderId);
        orderEvent.Item.Should().BeEquivalentTo(itemSnapshot);
        orderEvent.NewTotalAmount.Should().Be(newTotalAmount);
        orderEvent.ChangedAt.Should().Be(changedAt);
    }
}
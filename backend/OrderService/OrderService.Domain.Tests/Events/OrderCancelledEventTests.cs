using FluentAssertions;
using OrderService.Domain.Events;
using OrderService.Domain.Tests.Fixtures;

namespace OrderService.Domain.Tests.Events;

public class OrderCancelledEventTests(OrderFixture fixture) : IClassFixture<OrderFixture>
{
    [Fact]
    public void OrderCancelledEvent_ShouldInitializeAllProperties()
    {
        var orderId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        const string reason = "Customer requested cancellation";
        var cancelledBy = Guid.NewGuid();
        var cancelledAt = DateTime.UtcNow;

        var orderEvent = new OrderCancelledEvent(
            orderId,
            customerId,
            reason,
            cancelledBy,
            cancelledAt);

        orderEvent.OrderId.Should().Be(orderId);
        orderEvent.CustomerId.Should().Be(customerId);
        orderEvent.Reason.Should().Be(reason);
        orderEvent.CancelledBy.Should().Be(cancelledBy);
        orderEvent.CancelledAt.Should().Be(cancelledAt);
    }

    [Fact]
    public void OrderCancelledEvent_WithNullCancelledBy_ShouldSucceed()
    {
        var orderId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        var orderEvent = new OrderCancelledEvent(
            orderId,
            customerId,
            "System cancellation",
            null,
            DateTime.UtcNow);

        orderEvent.CancelledBy.Should().BeNull();
    }
}
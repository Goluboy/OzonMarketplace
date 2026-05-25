using FluentAssertions;
using OrderService.Domain.Events;

namespace OrderService.Domain.Tests.Events;

public class OrderItemRemovedEventTests
{
    [Fact]
    public void OrderItemRemovedEvent_ShouldInitializeAllProperties()
    {
        var orderId = Guid.NewGuid();
        var orderItemId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        const int quantity = 2;
        const decimal refundedAmount = 50.00m;
        const decimal newTotalAmount = 100.00m;
        var changedAt = DateTime.UtcNow;

        var orderEvent = new OrderItemRemovedEvent(
            orderId,
            orderItemId,
            productId,
            quantity,
            refundedAmount,
            newTotalAmount,
            changedAt);
        
        orderEvent.OrderId.Should().Be(orderId);
        orderEvent.OrderItemId.Should().Be(orderItemId);
        orderEvent.ProductId.Should().Be(productId);
        orderEvent.Quantity.Should().Be(quantity);
        orderEvent.RefundedAmount.Should().Be(refundedAmount);
        orderEvent.NewTotalAmount.Should().Be(newTotalAmount);
        orderEvent.ChangedAt.Should().Be(changedAt);
    }
}
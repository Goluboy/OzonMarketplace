using FluentAssertions;
using OrderService.Domain.Events;
using OrderService.Domain.Tests.Fixtures;

namespace OrderService.Domain.Tests.Events;

public class OrderCreatedEventTests(OrderFixture fixture) : IClassFixture<OrderFixture>
{
    [Fact]
    public void OrderCreatedEvent_ShouldInitializeAllProperties()
    {
        var order = fixture.CreateValidOrder();

        var orderEvent = new OrderCreatedEvent(
            order.Id,
            order.CustomerId,
            order.CustomerName,
            order.CustomerEmail,
            order.TotalAmount,
            order.DeliveryAddress,
            order.Items.Select(i => i.ToSnapshot()).ToList(),
            order.CreatedAt);

        orderEvent.OrderId.Should().Be(order.Id);
        orderEvent.CustomerId.Should().Be(order.CustomerId);
        orderEvent.CustomerName.Should().Be(order.CustomerName);
        orderEvent.CustomerEmail.Should().Be(order.CustomerEmail);
        orderEvent.TotalAmount.Should().Be(order.TotalAmount);
        orderEvent.DeliveryAddress.Should().Be(order.DeliveryAddress);
        orderEvent.Items.Should().BeEquivalentTo(order.Items);
        orderEvent.CreatedAt.Should().Be(order.CreatedAt);
        orderEvent.EventId.Should().NotBeEmpty();
        orderEvent.OccurredOn.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void OrderCreatedEvent_WithNullDeliveryAddress_ShouldSucceed()
    {
        var orderId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var items = new List<OrderItemSnapshot>();

        var orderEvent = new OrderCreatedEvent(
            orderId,
            customerId,
            "Test User",
            "test@example.com",
            100.00m,
            null,
            items,
            DateTime.UtcNow);

        orderEvent.DeliveryAddress.Should().BeNull();
    }
}
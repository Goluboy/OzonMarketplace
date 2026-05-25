using FluentAssertions;
using OrderService.Domain.Entities;
using OrderService.Domain.Tests.Fixtures;
using OrderService.Domain.ValueObjects;

namespace OrderService.Domain.Tests.Entities;

public class OrderStatusHistoryTests : IClassFixture<OrderFixture>
{
    [Fact]
    public void Create_WithValidData_ShouldCreateOrderStatusHistory()
    {
        var orderId = Guid.NewGuid();
        const OrderStatus oldStatus = OrderStatus.Created;
        const OrderStatus newStatus = OrderStatus.Paid;
        var changedBy = Guid.NewGuid();
        const string comment = "Payment processed";

        var history = OrderStatusHistory.Create(orderId, oldStatus, newStatus, changedBy, comment);

        history.Should().NotBeNull();
        history.Id.Should().NotBeEmpty();
        history.OrderId.Should().Be(orderId);
        history.OldStatus.Should().Be(oldStatus);
        history.NewStatus.Should().Be(newStatus);
        history.ChangedBy.Should().Be(changedBy);
        history.Comment.Should().Be(comment);
        history.ChangedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Create_WithNullChangedBy_ShouldSucceed()
    {
        var orderId = Guid.NewGuid();

        var history = OrderStatusHistory.Create(orderId, OrderStatus.Created, OrderStatus.Paid);

        history.Should().NotBeNull();
        history.ChangedBy.Should().BeNull();
        history.Comment.Should().BeNull();
    }

    [Fact]
    public void Create_WithSameOldAndNewStatus_ShouldSetOldStatusToNull()
    {
        var orderId = Guid.NewGuid();
        const OrderStatus status = OrderStatus.Paid;

        var history = OrderStatusHistory.Create(orderId, status, status);

        history.OldStatus.Should().BeNull();
        history.NewStatus.Should().Be(status);
    }

    [Fact]
    public void Create_ShouldGenerateNewId()
    {
        var orderId = Guid.NewGuid();

        var history1 = OrderStatusHistory.Create(orderId, OrderStatus.Created, OrderStatus.Paid);
        var history2 = OrderStatusHistory.Create(orderId, OrderStatus.Created, OrderStatus.Paid);

        history1.Id.Should().NotBe(history2.Id);
    }
}
using FluentAssertions;
using OrderService.Domain.Entities;
using OrderService.Domain.Events;
using OrderService.Domain.Tests.Fixtures;
using OrderService.Domain.ValueObjects;

namespace OrderService.Domain.Tests.Entities;

public class OrderTests(OrderFixture fixture) : IClassFixture<OrderFixture>
{
    [Fact]
    public void Create_WithValidData_ShouldCreateOrder()
    {
        var items = fixture.CreateOrderItems(2);

        var order = Order.Create(
            fixture.CustomerId,
            fixture.CustomerName,
            fixture.CustomerEmail,
            fixture.DeliveryAddress,
            items);

        order.Should().NotBeNull();
        order.CustomerId.Should().Be(fixture.CustomerId);
        order.CustomerName.Should().Be(fixture.CustomerName);
        order.CustomerEmail.Value.Should().Be(fixture.CustomerEmail.ToLower());
        order.DeliveryAddress?.AddressLine.Should().Be(fixture.DeliveryAddress);
        order.Status.Should().Be(OrderStatus.Created);
        order.Items.Count.Should().Be(2);
        order.Version.Should().Be(1);
        order.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        order.DomainEvents.Count.Should().Be(1);
        order.DomainEvents.First().Should().BeOfType<OrderCreatedEvent>();
    }

    [Fact]
    public void Create_WithNullCustomerName_ShouldThrowArgumentNullException()
    {
        var items = fixture.CreateOrderItems();

        var act = () => Order.Create(
            fixture.CustomerId,
            null!,
            fixture.CustomerEmail,
            fixture.DeliveryAddress,
            items);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("customerName");
    }

    [Fact]
    public void Create_WithEmptyItems_ShouldThrowArgumentException()
    {
        var items = Enumerable.Empty<OrderItem>();

        var act = () => Order.Create(
            fixture.CustomerId,
            fixture.CustomerName,
            fixture.CustomerEmail,
            fixture.DeliveryAddress,
            items);

        act.Should().Throw<ArgumentException>()
            .WithMessage("Order must contain at least one item (Parameter 'itemsList')");
    }

    [Fact]
    public void Create_ShouldSetOrderIdOnAllItems()
    {
        var items = fixture.CreateOrderItems(3);

        var order = Order.Create(
            fixture.CustomerId,
            fixture.CustomerName,
            fixture.CustomerEmail,
            fixture.DeliveryAddress,
            items);

        foreach (var item in order.Items)
        {
            item.OrderId.Should().Be(order.Id);
        }
    }

    [Fact]
    public void Create_ShouldRecalculateTotal()
    {
        var items = new List<OrderItem>
        {
            OrderItem.Create(Guid.NewGuid(), "Item1", 2, 10.00m),
            OrderItem.Create(Guid.NewGuid(), "Item2", 3, 15.00m)
        };

        var order = Order.Create(
            fixture.CustomerId,
            fixture.CustomerName,
            fixture.CustomerEmail,
            fixture.DeliveryAddress,
            items);

        order.TotalAmount.Amount.Should().Be(65.00m);
    }

    [Fact]
    public void Create_WithNullDeliveryAddress_ShouldSucceed()
    {
        var items = fixture.CreateOrderItems();

        var order = Order.Create(
            fixture.CustomerId,
            fixture.CustomerName,
            fixture.CustomerEmail,
            null,
            items);

        order.Should().NotBeNull();
        order.DeliveryAddress.Should().BeNull();
    }

    [Fact]
    public void AddItem_WhenOrderCanBeModified_ShouldAddItem()
    {
        var order = fixture.CreateValidOrder();
        var newItem = fixture.CreateOrderItem();
        var initialCount = order.Items.Count;
        var initialTotal = order.TotalAmount;

        order.AddItem(newItem);

        order.Items.Count.Should().Be(initialCount + 1);
        order.TotalAmount.Amount.Should().BeGreaterThan(initialTotal);
        order.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        order.Version.Should().Be(2);
        order.DomainEvents.Last().Should().BeOfType<OrderItemAddedEvent>();
    }

    [Theory]
    [InlineData(OrderStatus.Assembling)]
    [InlineData(OrderStatus.Shipping)]
    [InlineData(OrderStatus.Delivered)]
    [InlineData(OrderStatus.Cancelled)]
    public void AddItem_WhenOrderCannotBeModified_ShouldThrowInvalidOperationException(OrderStatus status)
    {
        var order = fixture.CreateValidOrder();
        fixture.ChangeOrderStatus(order, status);
        var newItem = fixture.CreateOrderItem();

        var act = () => order.AddItem(newItem);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"Cannot add items to order with status '{status}'");
    }

    [Fact]
    public void AddItem_WithNullItem_ShouldThrowArgumentNullException()
    {
        var order = fixture.CreateValidOrder();

        var act = () => order.AddItem(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RemoveItem_WhenOrderCanBeModified_ShouldRemoveItem()
    {
        var order = fixture.CreateValidOrder();
        var itemToRemove = order.Items.First();
        var initialCount = order.Items.Count;
        var refundedAmount = itemToRemove.Subtotal;

        order.RemoveItem(itemToRemove.Id);

        order.Items.Count.Should().Be(initialCount - 1);
        order.Items.Should().NotContain(i => i.Id == itemToRemove.Id);
        order.TotalAmount.Amount.Should().BeLessThan(order.TotalAmount + refundedAmount);
        order.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        order.Version.Should().Be(2);
        order.DomainEvents.Last().Should().BeOfType<OrderItemRemovedEvent>();
    }

    [Theory]
    [InlineData(OrderStatus.Assembling)]
    [InlineData(OrderStatus.Shipping)]
    [InlineData(OrderStatus.Delivered)]
    [InlineData(OrderStatus.Cancelled)]
    public void RemoveItem_WhenOrderCannotBeModified_ShouldThrowInvalidOperationException(OrderStatus status)
    {
        var order = fixture.CreateValidOrder();
        fixture.ChangeOrderStatus(order, status);
        var itemToRemove = order.Items.First();

        var act = () => order.RemoveItem(itemToRemove.Id);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"Cannot remove items from order with status '{status}'");
    }

    [Fact]
    public void RemoveItem_WithNonExistentItemId_ShouldThrowInvalidOperationException()
    {
        var order = fixture.CreateValidOrder();
        var nonExistentId = Guid.NewGuid();

        var act = () => order.RemoveItem(nonExistentId);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"Item '{nonExistentId}' not found in order");
    }

    [Fact]
    public void Cancel_WhenOrderIsNotCancelled_ShouldChangeStatusToCancelled()
    {
        var order = fixture.CreateValidOrder();

        order.Cancel(Guid.NewGuid(), "Test reason");

        order.Status.Should().Be(OrderStatus.Cancelled);
        order.CancelledAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        order.DomainEvents.Should().Contain(e => e is OrderStatusChangedEvent);
    }

    [Fact]
    public void Cancel_WhenOrderAlreadyCancelled_ShouldNotChangeStatus()
    {
        var order = fixture.CreateValidOrder();
        order.Cancel(Guid.NewGuid(), "First cancellation");
        var cancelledAt = order.CancelledAt;

        order.Cancel(Guid.NewGuid(), "Second reason");

        order.Status.Should().Be(OrderStatus.Cancelled);
        order.CancelledAt.Should().Be(cancelledAt);
    }

    [Theory]
    [InlineData(OrderStatus.Created, OrderStatus.Paid, true)]
    [InlineData(OrderStatus.Created, OrderStatus.Cancelled, true)]
    [InlineData(OrderStatus.Paid, OrderStatus.Assembling, true)]
    [InlineData(OrderStatus.Paid, OrderStatus.Cancelled, true)]
    [InlineData(OrderStatus.Assembling, OrderStatus.Shipping, true)]
    [InlineData(OrderStatus.Assembling, OrderStatus.Cancelled, true)]
    [InlineData(OrderStatus.Shipping, OrderStatus.Delivered, true)]
    [InlineData(OrderStatus.Shipping, OrderStatus.Cancelled, true)]
    [InlineData(OrderStatus.Created, OrderStatus.Assembling, false)]
    [InlineData(OrderStatus.Created, OrderStatus.Shipping, false)]
    [InlineData(OrderStatus.Created, OrderStatus.Delivered, false)]
    [InlineData(OrderStatus.Paid, OrderStatus.Shipping, false)]
    [InlineData(OrderStatus.Paid, OrderStatus.Delivered, false)]
    [InlineData(OrderStatus.Assembling, OrderStatus.Delivered, false)]
    [InlineData(OrderStatus.Delivered, OrderStatus.Created, false)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Created, false)]
    public void ChangeStatus_ValidTransition_ShouldUpdateStatus(OrderStatus current, OrderStatus next, bool shouldSucceed)
    {
        var order = fixture.CreateValidOrder();
        fixture.ForceSetStatus(order, current);

        var act = () => order.ChangeStatus(next, Guid.NewGuid(), "Test comment");

        if (shouldSucceed)
        {
            act.Should().NotThrow();
            order.Status.Should().Be(next);
            order.Version.Should().BeGreaterThan(1);
        }
        else
        {
            act.Should().Throw<InvalidOperationException>()
                .WithMessage($"Invalid status transition: '{current}' to '{next}'");
        }
    }

    [Fact]
    public void ChangeStatus_SameStatus_ShouldNotThrow()
    {
        var order = fixture.CreateValidOrder();
        var currentStatus = order.Status;

        var history = order.ChangeStatus(currentStatus);

        order.Status.Should().Be(currentStatus);
        history.Should().NotBeNull();
        history.OldStatus.Should().BeNull();
        history.NewStatus.Should().Be(currentStatus);
    }

    [Fact]
    public void IsOwnedBy_WithCorrectCustomerId_ShouldReturnTrue()
    {
        var order = fixture.CreateValidOrder();

        order.IsOwnedBy(fixture.CustomerId).Should().BeTrue();
    }

    [Fact]
    public void IsOwnedBy_WithWrongCustomerId_ShouldReturnFalse()
    {
        var order = fixture.CreateValidOrder();
        var wrongCustomerId = Guid.NewGuid();

        order.IsOwnedBy(wrongCustomerId).Should().BeFalse();
    }

    [Fact]
    public void CloneOrder_ShouldCreateDeepCopy()
    {
        var order = fixture.CreateValidOrder();
        order.ChangeStatus(OrderStatus.Paid);

        var clone = order.CloneOrder();

        clone.Should().NotBeSameAs(order);
        clone.Id.Should().Be(order.Id);
        clone.CustomerId.Should().Be(order.CustomerId);
        clone.CustomerName.Should().Be(order.CustomerName);
        clone.CustomerEmail.Should().Be(order.CustomerEmail);
        clone.DeliveryAddress.Should().Be(order.DeliveryAddress);
        clone.Status.Should().Be(order.Status);
        clone.TotalAmount.Should().Be(order.TotalAmount);
        clone.Version.Should().Be(order.Version);
        clone.Items.Count.Should().Be(order.Items.Count);

        for (int i = 0; i < order.Items.Count; i++)
        {
            clone.Items.ElementAt(i).Should().NotBeSameAs(order.Items.ElementAt(i));
            clone.Items.ElementAt(i).Id.Should().Be(order.Items.ElementAt(i).Id);
        }
    }

    [Fact]
    public void Equals_SameReference_ShouldReturnTrue()
    {
        var order = fixture.CreateValidOrder();

        order.Equals(order).Should().BeTrue();
    }

    [Fact]
    public void Equals_SameIdDifferentInstance_ShouldReturnTrue()
    {
        var order = fixture.CreateValidOrder();
        var clone = order.CloneOrder();

        order.Equals(clone).Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentId_ShouldReturnFalse()
    {
        var order1 = fixture.CreateValidOrder();
        var order2 = fixture.CreateValidOrder();

        order1.Equals(order2).Should().BeFalse();
    }

    [Fact]
    public void Equals_Null_ShouldReturnFalse()
    {
        var order = fixture.CreateValidOrder();

        order.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void CanBeModified_WhenStatusIsCreated_ShouldReturnTrue()
    {
        var order = fixture.CreateValidOrder();

        order.CanBeModified().Should().BeTrue();
    }

    [Fact]
    public void CanBeModified_WhenStatusIsPaid_ShouldReturnTrue()
    {
        var order = fixture.CreateValidOrder();
        order.ChangeStatus(OrderStatus.Paid);

        order.CanBeModified().Should().BeTrue();
    }

    [Theory]
    [InlineData(OrderStatus.Assembling)]
    [InlineData(OrderStatus.Shipping)]
    [InlineData(OrderStatus.Delivered)]
    [InlineData(OrderStatus.Cancelled)]
    public void CanBeModified_WhenStatusIsNotModifiable_ShouldReturnFalse(OrderStatus status)
    {
        var order = fixture.CreateValidOrder();
        fixture.ChangeOrderStatus(order, status);

        order.CanBeModified().Should().BeFalse();
    }

    [Fact]
    public void ClearDomainEvents_ShouldClearAllEvents()
    {
        var order = fixture.CreateValidOrder();
        order.ChangeStatus(OrderStatus.Paid);
        order.DomainEvents.Count.Should().BeGreaterThan(0);

        order.ClearDomainEvents();

        order.DomainEvents.Count.Should().Be(0);
    }

    [Fact]
    public void Rehydrate_WithValidData_ShouldRestoreOrder()
    {
        // Arrange
        var orderId = OrderId.New();
        var customerId = Guid.NewGuid();
        var customerName = "John Doe";
        var customerEmail = new Email("john@example.com");
        var deliveryAddress = DeliveryAddress.Create("123 Main St");
        var status = OrderStatus.Paid;
        var totalAmount = new Money(100m);
        var createdAt = DateTime.UtcNow;
        var updatedAt = DateTime.UtcNow.AddMinutes(10);
        var cancelledAt = (DateTime?)null;
        var PaidAt = (DateTime?)null;
        var version = 2;
        var items = new List<OrderItem>
        {
            OrderItem.Rehydrate(Guid.NewGuid(), orderId, Guid.NewGuid(), "Product 1", 1, new Money(50m), new Money(50m), createdAt, null),
            OrderItem.Rehydrate(Guid.NewGuid(), orderId, Guid.NewGuid(), "Product 2", 1, new Money(50m), new Money(50m), createdAt, null)
        };

        // Act
        var order = Order.Rehydrate(
            orderId,
            customerId,
            customerName,
            customerEmail,
            deliveryAddress,
            status,
            totalAmount,
            createdAt,
            updatedAt,
            PaidAt,
            cancelledAt,
            version,
            items);

        // Assert
        order.Should().NotBeNull();
        order.Id.Should().Be(orderId);
        order.CustomerId.Should().Be(customerId);
        order.CustomerName.Should().Be(customerName);
        order.CustomerEmail.Should().Be(customerEmail);
        order.DeliveryAddress.Should().Be(deliveryAddress);
        order.Status.Should().Be(status);
        order.TotalAmount.Should().Be(totalAmount);
        order.CreatedAt.Should().Be(createdAt);
        order.UpdatedAt.Should().Be(updatedAt);
        order.CancelledAt.Should().Be(cancelledAt);
        order.Version.Should().Be(version);
        order.Items.Count.Should().Be(2);
        order.Items.Should().BeEquivalentTo(items);
    }

    [Fact]
    public void Rehydrate_WithNullDeliveryAddress_ShouldRestoreCorrectly()
    {
        // Arrange
        var orderId = OrderId.New();
        var customerId = Guid.NewGuid();
        var customerName = "John Doe";
        var customerEmail = new Email("john@example.com");
        var status = OrderStatus.Created;
        var totalAmount = new Money(100m);
        var createdAt = DateTime.UtcNow;
        var items = new List<OrderItem>();

        // Act
        var order = Order.Rehydrate(
            orderId,
            customerId,
            customerName,
            customerEmail,
            null,
            status,
            totalAmount,
            createdAt,
            null,
            null,
            null,
            1,
            items);

        // Assert
        order.DeliveryAddress.Should().BeNull();
    }

    [Fact]
    public void MarkAsPaid_WhenAllItemsReserved_ShouldChangeStatusToAssembling()
    {
        var order = fixture.CreateValidOrder();
        var reservedItems = order.Items
            .Select(i => (i.ProductId, i.Quantity))
            .ToList();

        order.MarkItemsAsReserved(reservedItems);

        order.MarkAsPaid();

        order.Status.Should().Be(OrderStatus.Assembling);
        order.PaidAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        order.Version.Should().BeGreaterThan(1);
        order.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        order.DomainEvents.Should().Contain(e => e is OrderStatusChangedEvent);
    }

    [Fact]
    public void MarkAsPaid_WhenAllItemsReserved_ShouldAutoTransitionToAssembling()
    {
        var order = fixture.CreateValidOrder();
        var reservedItems = order.Items
            .Select(i => (i.ProductId, i.Quantity))
            .ToList();
        order.MarkItemsAsReserved(reservedItems);

        order.MarkAsPaid();

        order.Status.Should().Be(OrderStatus.Assembling);
    }

    [Fact]
    public void MarkAsPaid_WhenAlreadyPaid_ShouldBeIdempotent()
    {
        var order = fixture.CreateValidOrder();
        var reservedItems = order.Items.Select(i => (i.ProductId, i.Quantity)).ToList();
        order.MarkItemsAsReserved(reservedItems);
        order.MarkAsPaid();

        var paidAt = order.PaidAt;
        var version = order.Version;

        order.MarkAsPaid();

        order.Status.Should().Be(OrderStatus.Assembling);
        order.PaidAt.Should().Be(paidAt);
        order.Version.Should().Be(version);
    }

    [Fact]
    public void MarkAsPaid_WhenNotAllItemsReserved_ShouldThrowInvalidOperationException()
    {
        var order = fixture.CreateValidOrder();
        var partialReserved = new List<(Guid, int)>
    {
        (order.Items.First().ProductId, order.Items.First().Quantity)
    };
        order.MarkItemsAsReserved(partialReserved);

        var act = () => order.MarkAsPaid();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"Cannot mark order '{order.Id}' as Paid: not all items are reserved.*");
    }

    [Fact]
    public void MarkAsPaid_WhenNoItemsReserved_ShouldThrowInvalidOperationException()
    {
        var order = fixture.CreateValidOrder();

        var act = () => order.MarkAsPaid();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"Cannot mark order '{order.Id}' as Paid: not all items are reserved.*");
    }

    [Fact]
    public void SetTotalAmount_WhenStatusIsCreated_ShouldUpdateAmount()
    {
        var order = fixture.CreateValidOrder();
        var oldTotal = order.TotalAmount;
        var newTotal = new Money(999.99m, "RUB");

        order.SetTotalAmount(newTotal);

        order.TotalAmount.Amount.Should().Be(999.99m);
        order.TotalAmount.Currency.Should().Be("RUB");
        order.TotalAmount.Should().NotBe(oldTotal);
        order.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        order.Version.Should().Be(2);
    }

    [Theory]
    [InlineData(OrderStatus.Paid)]
    [InlineData(OrderStatus.Assembling)]
    [InlineData(OrderStatus.Shipping)]
    [InlineData(OrderStatus.Delivered)]
    public void SetTotalAmount_WhenStatusIsNotCreated_ShouldThrowInvalidOperationException(OrderStatus status)
    {
        var order = fixture.CreateValidOrder();
        fixture.ForceSetStatus(order, status);
        var newTotal = new Money(999.99m, "RUB");

        var act = () => order.SetTotalAmount(newTotal);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"Cannot update total amount for order with status '{status}'.*");
    }

    [Fact]
    public void SetTotalAmount_WhenStatusIsCancelled_ShouldThrowInvalidOperationException()
    {
        var order = fixture.CreateValidOrder();
        order.Cancel(Guid.NewGuid(), "Test reason");

        var act = () => order.SetTotalAmount(new Money(100m, "RUB"));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot update total amount for cancelled order");
    }

    [Fact]
    public void SetTotalAmount_WithNegativeAmount_ShouldThrowArgumentException()
    {
        var order = fixture.CreateValidOrder();

        var act = () => order.SetTotalAmount(new Money(-100m, "RUB"));

        act.Should().Throw<ArgumentException>()
            .WithMessage("Value must be non-negative (Parameter 'value')");
    }

    [Fact]
    public void SetTotalAmount_WithZeroAmount_ShouldSucceed()
    {
        var order = fixture.CreateValidOrder();

        order.SetTotalAmount(new Money(0m, "RUB"));

        order.TotalAmount.Amount.Should().Be(0m);
    }

    [Fact]
    public void MarkItemsAsReserved_WhenStatusIsPaid_ShouldThrowInvalidOperationException()
    {
        var order = fixture.CreateValidOrder();
        fixture.ForceSetStatus(order, OrderStatus.Paid);
        var reservedItems = order.Items.Select(i => (i.ProductId, i.Quantity)).ToList();

        var act = () => order.MarkItemsAsReserved(reservedItems);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot reserve items for order with status 'Paid'. Expected: 'Created'.");
    }

    [Fact]
    public void MarkItemsAsReserved_WhenStatusIsCreated_ShouldNotTransitionToAssembling()
    {
        var order = fixture.CreateValidOrder();
        var reservedItems = order.Items.Select(i => (i.ProductId, i.Quantity)).ToList();

        order.MarkItemsAsReserved(reservedItems);

        order.Status.Should().Be(OrderStatus.Created);
    }

    [Fact]
    public void FullSagaFlow_CreateReservePrice_ShouldTransitionThroughAllStatuses()
    {
        var order = fixture.CreateValidOrder();
        order.Status.Should().Be(OrderStatus.Created);

        order.SetTotalAmount(new Money(1500m, "RUB"));
        order.TotalAmount.Amount.Should().Be(1500m);
        order.Status.Should().Be(OrderStatus.Created);

        var reservedItems = order.Items.Select(i => (i.ProductId, i.Quantity)).ToList();
        order.MarkItemsAsReserved(reservedItems);
        order.AllItemsReserved().Should().BeTrue();

        order.MarkAsPaid();

        order.PaidAt.Should().NotBeNull();
        order.Status.Should().Be(OrderStatus.Assembling);
        order.Version.Should().BeGreaterThan(3);
    }

    [Fact]
    public void FullSagaFlow_FailedReservation_ShouldAllowCancellation()
    {
        var order = fixture.CreateValidOrder();
        var reservedItems = order.Items.Select(i => (i.ProductId, i.Quantity)).ToList();
        order.MarkItemsAsReserved(reservedItems);

        order.ReleaseAllReservations();
        order.ForceCancel("Stock reservation failed after initial reservation");

        order.Status.Should().Be(OrderStatus.Cancelled);
        order.AllItemsReserved().Should().BeFalse();
        order.CancelledAt.Should().NotBeNull();
    }

    [Fact]
    public void AllItemsReserved_WhenNoItems_ShouldReturnFalse()
    {
        var order = Order.Create(
            fixture.CustomerId,
            fixture.CustomerName,
            fixture.CustomerEmail,
            fixture.DeliveryAddress,
            new List<OrderItem> { fixture.CreateOrderItem() }); // хотя бы 1 item при создании

        // Очищаем items через RemoveItem для проверки edge case
        var firstItem = order.Items.First();
        order.RemoveItem(firstItem.Id);

        order.AllItemsReserved().Should().BeFalse();
    }

    [Fact]
    public void AllItemsReserved_WhenAllItemsReserved_ShouldReturnTrue()
    {
        var order = fixture.CreateValidOrder();
        var reservedItems = order.Items.Select(i => (i.ProductId, i.Quantity)).ToList();

        order.MarkItemsAsReserved(reservedItems);

        order.AllItemsReserved().Should().BeTrue();
    }

    [Fact]
    public void AllItemsReserved_WhenPartialReservation_ShouldReturnFalse()
    {
        var order = fixture.CreateValidOrder();
        if (order.Items.Count < 2)
        {
            // Добавим ещё один item для корректного теста
            order.AddItem(fixture.CreateOrderItem());
        }

        var partialReserved = new List<(Guid, int)>
    {
        (order.Items.First().ProductId, order.Items.First().Quantity)
    };
        order.MarkItemsAsReserved(partialReserved);

        order.AllItemsReserved().Should().BeFalse();
    }

    [Fact]
    public void ReleaseAllReservations_ShouldReleaseAllItems()
    {
        var order = fixture.CreateValidOrder();
        var reservedItems = order.Items.Select(i => (i.ProductId, i.Quantity)).ToList();
        order.MarkItemsAsReserved(reservedItems);
        order.AllItemsReserved().Should().BeTrue();

        order.ReleaseAllReservations();

        order.AllItemsReserved().Should().BeFalse();
        order.Items.Should().OnlyContain(i => !i.IsReserved);
        order.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        order.Version.Should().BeGreaterThan(1);
    }

    [Fact]
    public void ReleaseAllReservations_WhenNoReservations_ShouldBeIdempotent()
    {
        var order = fixture.CreateValidOrder();
        var versionBefore = order.Version;

        order.ReleaseAllReservations();

        // Version всё равно увеличивается (UpdatedAt обновляется)
        order.Version.Should().BeGreaterThan(versionBefore);
        order.AllItemsReserved().Should().BeFalse();
    }
}
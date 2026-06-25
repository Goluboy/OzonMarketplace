using FluentAssertions;
using OrderService.Domain.Entities;
using OrderService.Domain.Tests.Fixtures;
using OrderService.Domain.ValueObjects;

namespace OrderService.Domain.Tests.Entities;

public class OrderItemTests(OrderFixture fixture) : IClassFixture<OrderFixture>
{
    [Fact]
    public void Create_WithValidData_ShouldCreateOrderItem()
    {
        var productId = Guid.NewGuid();
        const string productName = "Test Product";
        const int quantity = 5;
        const decimal price = 10.50m;

        var orderItem = OrderItem.Create(productId, productName, quantity, price);

        orderItem.Should().NotBeNull();
        orderItem.Id.Should().NotBeEmpty();
        orderItem.ProductId.Should().Be(productId);
        orderItem.ProductName.Should().Be(productName);
        orderItem.Quantity.Should().Be(quantity);
        orderItem.PriceAtPurchase.Amount.Should().Be(price);
        orderItem.PriceAtPurchase.Currency.Should().Be("RUB");
        orderItem.Subtotal.Amount.Should().Be(52.50m);
        orderItem.Subtotal.Currency.Should().Be("RUB");
        orderItem.OrderId.Should().Be(null);
        orderItem.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void Create_WithInvalidQuantity_ShouldThrowArgumentException(int quantity)
    {
        var productId = Guid.NewGuid();
        const string productName = "Test Product";
        const decimal price = 10.50m;

        var act = () => OrderItem.Create(productId, productName, quantity, price);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*quantity*");
    }

    [Fact]
    public void Create_WithNegativePrice_ShouldThrowArgumentException()
    {
        var productId = Guid.NewGuid();
        const string productName = "Test Product";
        const int quantity = 5;
        const decimal price = -1.00m;

        var act = () => OrderItem.Create(productId, productName, quantity, price);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*price*");
    }

    [Fact]
    public void Create_WithZeroPrice_ShouldSucceed()
    {
        var productId = Guid.NewGuid();
        const string productName = "Free Product";
        const int quantity = 5;
        const decimal price = 0m;

        var orderItem = OrderItem.Create(productId, productName, quantity, price);

        orderItem.Should().NotBeNull();
        orderItem.PriceAtPurchase.Amount.Should().Be(0m);
        orderItem.PriceAtPurchase.Currency.Should().Be("RUB");
        orderItem.Subtotal.Currency.Should().Be("RUB");
        orderItem.Subtotal.Amount.Should().Be(0m);
    }

    [Fact]
    public void SetOrderId_WhenNotSet_ShouldSetOrderId()
    {
        var orderItem = fixture.CreateOrderItem();
        var orderId = OrderId.New();

        orderItem.SetOrderId(orderId);

        orderItem.OrderId.Should().Be(orderId);
        orderItem.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void SetOrderId_WhenAlreadySetToSameId_ShouldNotChange()
    {
        var orderItem = fixture.CreateOrderItem();
        var orderId = OrderId.New();
        orderItem.SetOrderId(orderId);
        var updatedAt = orderItem.UpdatedAt;

        orderItem.SetOrderId(orderId);

        orderItem.OrderId.Should().Be(orderId);
        orderItem.UpdatedAt.Should().Be(updatedAt);
    }

    [Fact]
    public void SetOrderId_WhenAlreadySetToDifferentId_ShouldThrowInvalidOperationException()
    {
        var orderItem = fixture.CreateOrderItem();
        var orderId1 = OrderId.New();
        var orderId2 = OrderId.New();
        orderItem.SetOrderId(orderId1);

        var act = () => orderItem.SetOrderId(orderId2);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Order already set");
    }

    [Fact]
    public void UpdateQuantity_WithValidQuantity_ShouldUpdateQuantityAndSubtotal()
    {
        var orderItem = fixture.CreateOrderItem(quantity: 5, price: 10.00m);
        const int newQuantity = 10;

        orderItem.UpdateQuantity(newQuantity);

        orderItem.Quantity.Should().Be(newQuantity);
        orderItem.Subtotal.Amount.Should().Be(100.00m);
        orderItem.Subtotal.Currency.Should().Be("RUB");
        orderItem.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void UpdateQuantity_WithInvalidQuantity_ShouldThrowArgumentException(int newQuantity)
    {
        var orderItem = fixture.CreateOrderItem();

        var act = () => orderItem.UpdateQuantity(newQuantity);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Количество должно быть больше нуля*");
    }

    [Fact]
    public void CloneItem_ShouldCreateCopyWithSameProperties()
    {
        var orderItem = fixture.CreateOrderItem();
        orderItem.SetOrderId(OrderId.New());

        var clone = orderItem.CloneItem();

        clone.Should().NotBeSameAs(orderItem);
        clone.Id.Should().Be(orderItem.Id);
        clone.OrderId.Should().Be(orderItem.OrderId);
        clone.ProductId.Should().Be(orderItem.ProductId);
        clone.ProductName.Should().Be(orderItem.ProductName);
        clone.Quantity.Should().Be(orderItem.Quantity);
        clone.PriceAtPurchase.Should().Be(orderItem.PriceAtPurchase);
        clone.Subtotal.Should().Be(orderItem.Subtotal);
    }

    [Fact]
    public void Equals_SameId_ShouldReturnTrue()
    {
        var orderItem = fixture.CreateOrderItem();
        var sameOrderItem = orderItem;

        orderItem.Equals(sameOrderItem).Should().BeTrue();
        (orderItem == sameOrderItem).Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentId_ShouldReturnFalse()
    {
        var orderItem1 = fixture.CreateOrderItem();
        var orderItem2 = fixture.CreateOrderItem();

        orderItem1.Equals(orderItem2).Should().BeFalse();
        (orderItem1 == orderItem2).Should().BeFalse();
    }

    [Fact]
    public void Rehydrate_WithValidData_ShouldRestoreOrderItem()
    {
        var id = Guid.NewGuid();
        var orderId = OrderId.New();
        var productId = Guid.NewGuid();
        const string productName = "Rehydrated Product";
        const int quantity = 2;
        var price = new Money(100m);
        var subtotal = new Money(200m);
        var createdAt = DateTime.UtcNow;
        var updatedAt = DateTime.UtcNow.AddMinutes(5);

        var item = OrderItem.Rehydrate(id, orderId, productId, productName, quantity, price, subtotal, createdAt,
            updatedAt, false);

        item.Should().NotBeNull();
        item.Id.Should().Be(id);
        item.OrderId.Should().Be(orderId);
        item.ProductId.Should().Be(productId);
        item.ProductName.Should().Be(productName);
        item.Quantity.Should().Be(quantity);
        item.PriceAtPurchase.Should().Be(price);
        item.Subtotal.Should().Be(subtotal);
        item.CreatedAt.Should().Be(createdAt);
        item.UpdatedAt.Should().Be(updatedAt);
    }

    [Fact]
    public void Rehydrate_WithNullOrderId_ShouldRestoreCorrectly()
    {
        var id = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var price = new Money(100m);
        var subtotal = new Money(100m);
        var createdAt = DateTime.UtcNow;

        var item = OrderItem.Rehydrate(id, null, productId, "Product", 1, price, subtotal, createdAt, null, false);

        item.OrderId.Should().BeNull();
        item.UpdatedAt.Should().BeNull();
    }
}
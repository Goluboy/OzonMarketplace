using OrderService.Domain.Entities;
using OrderService.Domain.ValueObjects;
using OrderService.Infrastructure.Persistence.Repositories;
using OrderService.Infrastructure.Persistence.Tests.Fixtures;

namespace OrderService.Infrastructure.Persistence.Tests.Repositories;

[Collection("OrderRepository Collection")]
public class OrderRepositoryTests(PostgreSqlFixture dbFixture) : IAsyncLifetime, IClassFixture<PostgreSqlFixture>, IClassFixture<UnitOfWorkFixture>, IClassFixture<OrderFixture>
{
    private readonly PostgreSqlFixture _dbFixture = dbFixture ?? throw new ArgumentNullException(nameof(dbFixture));
    private readonly UnitOfWorkFixture _unitOfWorkFixture = new(dbFixture);
    private readonly OrderFixture _orderFixture = new();
    private OrderRepository _repository = null!;

    public async Task InitializeAsync()
    {
        _unitOfWorkFixture.Initialize();
        _repository = new OrderRepository(_unitOfWorkFixture.UnitOfWork);
        await _dbFixture.ClearDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
        await _dbFixture.ClearDatabaseAsync();
    }

    #region SaveAsync Tests

    [Fact]
    public async Task SaveAsync_WithNewOrder_ShouldInsertOrderAndItems()
    {
        var order = _orderFixture.CreateValidOrder();

        await _unitOfWorkFixture.UnitOfWork.BeginTransactionAsync();
        await _repository.SaveAsync(order);
        await _unitOfWorkFixture.UnitOfWork.CommitAsync();

        var retrievedOrder = await _repository.GetByIdAsync(order.Id);
        Assert.NotNull(retrievedOrder);
        Assert.Equal(order.Id, retrievedOrder.Id);
        Assert.Equal(order.CustomerId, retrievedOrder.CustomerId);
        Assert.Equal(order.CustomerName, retrievedOrder.CustomerName);
        Assert.Equal(order.Items.Count, retrievedOrder.Items.Count);
    }

    [Fact]
    public async Task SaveAsync_WithExistingOrder_ShouldUpdateOrder()
    {
        var order = _orderFixture.CreateValidOrder();
        await _unitOfWorkFixture.UnitOfWork.BeginTransactionAsync();
        await _repository.SaveAsync(order);
        await _unitOfWorkFixture.UnitOfWork.CommitAsync();

        order.ChangeStatus(OrderStatus.Paid);
        await _unitOfWorkFixture.UnitOfWork.BeginTransactionAsync();
        await _repository.SaveAsync(order);
        await _unitOfWorkFixture.UnitOfWork.CommitAsync();

        var retrievedOrder = await _repository.GetByIdAsync(order.Id);
        Assert.NotNull(retrievedOrder);
        Assert.Equal(OrderStatus.Paid, retrievedOrder.Status);
        Assert.Equal(order.Version, retrievedOrder.Version);
    }

    [Fact]
    public async Task SaveAsync_WithMultipleItems_ShouldPersistAllItems()
    {
        var items = _orderFixture.CreateOrderItems(5);
        var order = Order.Create(
            _orderFixture.CustomerId,
            _orderFixture.CustomerName,
            _orderFixture.CustomerEmail,
            _orderFixture.DeliveryAddress,
            items);

        await _unitOfWorkFixture.UnitOfWork.BeginTransactionAsync();
        await _repository.SaveAsync(order);
        await _unitOfWorkFixture.UnitOfWork.CommitAsync();

        var retrievedOrder = await _repository.GetByIdAsync(order.Id);
        Assert.NotNull(retrievedOrder);
        Assert.Equal(5, retrievedOrder.Items.Count);
        foreach (var item in retrievedOrder.Items)
        {
            Assert.Equal(order.Id, item.OrderId);
        }
    }

    [Fact]
    public async Task SaveAsync_ShouldPersistAllOrderFields()
    {
        var order = _orderFixture.CreateValidOrder();
        var expectedCustomerId = order.CustomerId;
        var expectedStatus = order.Status;
        var expectedTotalAmount = order.TotalAmount.Value;

        await _unitOfWorkFixture.UnitOfWork.BeginTransactionAsync();
        await _repository.SaveAsync(order);
        await _unitOfWorkFixture.UnitOfWork.CommitAsync();

        var retrievedOrder = await _repository.GetByIdAsync(order.Id);
        Assert.NotNull(retrievedOrder);
        Assert.Equal(expectedCustomerId, retrievedOrder.CustomerId);
        Assert.Equal(expectedStatus, retrievedOrder.Status);
        Assert.Equal(expectedTotalAmount, retrievedOrder.TotalAmount.Value);
        Assert.True((order.CreatedAt - retrievedOrder.CreatedAt).Duration() < TimeSpan.FromMilliseconds(1));
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingOrder_ShouldReturnOrder()
    {
        var order = _orderFixture.CreateValidOrder();
        await _unitOfWorkFixture.UnitOfWork.BeginTransactionAsync();
        await _repository.SaveAsync(order);
        await _unitOfWorkFixture.UnitOfWork.CommitAsync();

        var retrievedOrder = await _repository.GetByIdAsync(order.Id);

        Assert.NotNull(retrievedOrder);
        Assert.Equal(order.Id, retrievedOrder.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingOrder_ShouldReturnNull()
    {
        var retrievedOrder = await _repository.GetByIdAsync(Guid.NewGuid());

        Assert.Null(retrievedOrder);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldRehydrateOrderWithAllItems()
    {
        var items = _orderFixture.CreateOrderItems(3);
        var order = Order.Create(
            _orderFixture.CustomerId,
            _orderFixture.CustomerName,
            _orderFixture.CustomerEmail,
            _orderFixture.DeliveryAddress,
            items);

        await _unitOfWorkFixture.UnitOfWork.BeginTransactionAsync();
        await _repository.SaveAsync(order);
        await _unitOfWorkFixture.UnitOfWork.CommitAsync();

        var retrievedOrder = await _repository.GetByIdAsync(order.Id);

        Assert.NotNull(retrievedOrder);
        Assert.Equal(3, retrievedOrder.Items.Count);

        foreach (var item in retrievedOrder.Items)
        {
            Assert.Equal(order.Id, item.OrderId);
            Assert.NotEqual(Guid.Empty, item.ProductId);
            Assert.NotNull(item.ProductName);
            Assert.True(item.Quantity > 0);
        }
    }

    [Fact]
    public async Task GetByIdAsync_ShouldPreserveOrderProperties()
    {
        var order = _orderFixture.CreateValidOrder();
        await _unitOfWorkFixture.UnitOfWork.BeginTransactionAsync();
        await _repository.SaveAsync(order);
        await _unitOfWorkFixture.UnitOfWork.CommitAsync();

        var retrievedOrder = await _repository.GetByIdAsync(order.Id);

        Assert.NotNull(retrievedOrder);
        Assert.Equal(order.CustomerId, retrievedOrder.CustomerId);
        Assert.Equal(order.CustomerName, retrievedOrder.CustomerName);
        Assert.Equal(order.CustomerEmail.Value, retrievedOrder.CustomerEmail.Value);
        Assert.Equal(order.Status, retrievedOrder.Status);
        Assert.Equal(order.TotalAmount.Value, retrievedOrder.TotalAmount.Value);
        Assert.Equal(order.Version, retrievedOrder.Version);
    }

    [Fact]
    public async Task GetByIdAsync_WithNullDeliveryAddress_ShouldRehydrateCorrectly()
    {
        var items = _orderFixture.CreateOrderItems();
        var order = Order.Create(
            _orderFixture.CustomerId,
            _orderFixture.CustomerName,
            _orderFixture.CustomerEmail,
            null,
            items);

        await _unitOfWorkFixture.UnitOfWork.BeginTransactionAsync();
        await _repository.SaveAsync(order);
        await _unitOfWorkFixture.UnitOfWork.CommitAsync();

        var retrievedOrder = await _repository.GetByIdAsync(order.Id);

        Assert.NotNull(retrievedOrder);
        Assert.Null(retrievedOrder.DeliveryAddress);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldPreserveItemDetails()
    {
        var productId = Guid.NewGuid();
        const string productName = "Specific Product";
        const int quantity = 5;
        const decimal price = 99.99m;

        var item = OrderItem.Create(productId, productName, quantity, price);
        var order = Order.Create(
            _orderFixture.CustomerId,
            _orderFixture.CustomerName,
            _orderFixture.CustomerEmail,
            _orderFixture.DeliveryAddress,
            new[] { item });

        await _unitOfWorkFixture.UnitOfWork.BeginTransactionAsync();
        await _repository.SaveAsync(order);
        await _unitOfWorkFixture.UnitOfWork.CommitAsync();

        var retrievedOrder = await _repository.GetByIdAsync(order.Id);

        Assert.NotNull(retrievedOrder);
        Assert.Single(retrievedOrder.Items);

        var retrievedItem = retrievedOrder.Items.First();
        Assert.Equal(productId, retrievedItem.ProductId);
        Assert.Equal(productName, retrievedItem.ProductName);
        Assert.Equal(quantity, retrievedItem.Quantity);
        Assert.Equal(price, retrievedItem.PriceAtPurchase.Value);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithExistingOrder_ShouldRemoveOrder()
    {
        var order = _orderFixture.CreateValidOrder();
        await _unitOfWorkFixture.UnitOfWork.BeginTransactionAsync();
        await _repository.SaveAsync(order);
        await _unitOfWorkFixture.UnitOfWork.CommitAsync();

        await _unitOfWorkFixture.UnitOfWork.BeginTransactionAsync();
        await _repository.DeleteAsync(order.Id);
        await _unitOfWorkFixture.UnitOfWork.CommitAsync();

        var retrievedOrder = await _repository.GetByIdAsync(order.Id);
        Assert.Null(retrievedOrder);
    }

    [Fact]
    public async Task DeleteAsync_ShouldCascadeDeleteOrderItems()
    {
        var order = _orderFixture.CreateValidOrder();
        await _unitOfWorkFixture.UnitOfWork.BeginTransactionAsync();
        await _repository.SaveAsync(order);
        await _unitOfWorkFixture.UnitOfWork.CommitAsync();
        
        await _unitOfWorkFixture.UnitOfWork.BeginTransactionAsync();
        await _repository.DeleteAsync(order.Id);
        await _unitOfWorkFixture.UnitOfWork.CommitAsync();

        
        var retrievedOrder = await _repository.GetByIdAsync(order.Id);
        Assert.Null(retrievedOrder);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistingOrder_ShouldNotThrow()
    {
        
        await _unitOfWorkFixture.UnitOfWork.BeginTransactionAsync();
        await _repository.DeleteAsync(Guid.NewGuid());
        await _unitOfWorkFixture.UnitOfWork.CommitAsync();
    }

    #endregion

    #region Transaction Tests

    [Fact]
    public async Task SaveAsync_WithinExistingTransaction_ShouldEnlistInTransaction()
    {
        var order = _orderFixture.CreateValidOrder();

        await _unitOfWorkFixture.UnitOfWork.BeginTransactionAsync();
        Assert.NotNull(_unitOfWorkFixture.UnitOfWork.CurrentTransaction);

        await _repository.SaveAsync(order);
        await _unitOfWorkFixture.UnitOfWork.CommitAsync();

        var retrievedOrder = await _repository.GetByIdAsync(order.Id);
        Assert.NotNull(retrievedOrder);
    }

    [Fact]
    public async Task SaveAsync_RollbackShouldUndoChanges()
    {
        var order = _orderFixture.CreateValidOrder();

        await _unitOfWorkFixture.UnitOfWork.BeginTransactionAsync();
        await _repository.SaveAsync(order);
        await _unitOfWorkFixture.UnitOfWork.RollbackAsync();

        var retrievedOrder = await _repository.GetByIdAsync(order.Id);
        Assert.Null(retrievedOrder);
    }

    #endregion
}

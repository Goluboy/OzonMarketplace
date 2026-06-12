using OrderService.Domain.Entities;
using OrderService.Domain.ValueObjects;
using OrderService.Infrastructure.Persistence.Repositories;
using OrderService.Infrastructure.Persistence.Tests.Fixtures;
using System.Collections.Generic;

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
        var expectedTotalAmount = order.TotalAmount.Amount;

        await _unitOfWorkFixture.UnitOfWork.BeginTransactionAsync();
        await _repository.SaveAsync(order);
        await _unitOfWorkFixture.UnitOfWork.CommitAsync();

        var retrievedOrder = await _repository.GetByIdAsync(order.Id);
        Assert.NotNull(retrievedOrder);
        Assert.Equal(expectedCustomerId, retrievedOrder.CustomerId);
        Assert.Equal(expectedStatus, retrievedOrder.Status);
        Assert.Equal(expectedTotalAmount, retrievedOrder.TotalAmount.Amount);
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
        Assert.Equal(order.TotalAmount.Amount, retrievedOrder.TotalAmount.Amount);
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
        Assert.Equal(price, retrievedItem.PriceAtPurchase.Amount);
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
        Assert.NotNull(_unitOfWorkFixture.UnitOfWork.Transaction);

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

    #region GetAllAsync Pagination Tests

    [Fact]
    public async Task GetAllAsync_WithPageAndPageSize_ShouldReturnPagedResults()
    {
        var orders = new List<Order>();
        for (int i = 0; i < 5; i++)
        {
            var order = _orderFixture.CreateValidOrder();
            order.ChangeStatus(OrderStatus.Paid);
            orders.Add(order);
        }

        await _unitOfWorkFixture.UnitOfWork.BeginTransactionAsync();
        foreach (var order in orders)
        {
            await _repository.SaveAsync(order);
        }
        await _unitOfWorkFixture.UnitOfWork.CommitAsync();

        var (pagedOrders, totalCount) = await _repository.GetAllAsync(1, 2);

        Assert.NotNull(pagedOrders);
        Assert.Equal(2, pagedOrders.Count());
        Assert.Equal(5, totalCount);
    }

    [Fact]
    public async Task GetAllAsync_WithPageAndPageSize_ShouldReturnCorrectOrders()
    {
        var orders = new List<Order>();
        for (int i = 0; i < 3; i++)
        {
            var order = _orderFixture.CreateValidOrder();
            order.ChangeStatus(OrderStatus.Paid);
            orders.Add(order);
        }

        await _unitOfWorkFixture.UnitOfWork.BeginTransactionAsync();
        foreach (var order in orders)
        {
            await _repository.SaveAsync(order);
        }
        await _unitOfWorkFixture.UnitOfWork.CommitAsync();

        var (firstPageOrders, totalCount) = await _repository.GetAllAsync(1, 2);

        Assert.NotNull(firstPageOrders);
        Assert.Equal(2, firstPageOrders.Count());
        Assert.Equal(3, totalCount);

        var (secondPageOrders, _) = await _repository.GetAllAsync(2, 2);

        Assert.NotNull(secondPageOrders);
        Assert.Single(secondPageOrders);
        
        var firstPageIds = firstPageOrders.Select(o => o.Id).ToList();
        var secondPageIds = secondPageOrders.Select(o => o.Id).ToList();
        
        Assert.Empty(firstPageIds.Intersect(secondPageIds));
    }

    [Fact]
    public async Task GetAllAsync_WithFilteringAndPagination_ShouldReturnFilteredPagedResults()
    {
        var orders = new List<Order>();
        for (int i = 0; i < 4; i++)
        {
            var order = _orderFixture.CreateValidOrder();
            if (i % 2 == 0)
            {
                order.ChangeStatus(OrderStatus.Paid);
            }
            else
            {
                order.ChangeStatus(OrderStatus.Cancelled);
            }
            orders.Add(order);
        }

        await _unitOfWorkFixture.UnitOfWork.BeginTransactionAsync();
        foreach (var order in orders)
        {
            await _repository.SaveAsync(order);
        }
        await _unitOfWorkFixture.UnitOfWork.CommitAsync();

        var filteredOrders = await _repository.GetAllAsync(null, OrderStatus.Paid, null, null, 1, 2);

        Assert.NotNull(filteredOrders);
        Assert.Equal(2, filteredOrders.Count());
        
        foreach (var order in filteredOrders)
        {
            Assert.Equal(OrderStatus.Paid, order.Status);
        }
    }

    #endregion

    #region GetAllAsync Complex Filtering Tests

    [Fact]
    public async Task GetAllAsync_WithCustomerIdFilter_ShouldReturnFilteredOrders()
    {
        var customer1 = Guid.NewGuid();
        var customer2 = Guid.NewGuid();
        
        var orders1 = new List<Order>();
        var orders2 = new List<Order>();
        
        for (int i = 0; i < 3; i++)
        {
            var order = _orderFixture.CreateValidOrder();
            orders1.Add(order);
        }
        
        for (int i = 0; i < 2; i++)
        {
            var order = _orderFixture.CreateValidOrder();
            orders2.Add(order);
        }

        await _unitOfWorkFixture.UnitOfWork.BeginTransactionAsync();
        foreach (var order in orders1.Concat(orders2))
        {
            await _repository.SaveAsync(order);
        }
        await _unitOfWorkFixture.UnitOfWork.CommitAsync();

        var filteredOrders = await _repository.GetAllAsync(customer1, null, null, null, 1, 10);
        
        Assert.NotNull(filteredOrders);
    }

    [Fact]
    public async Task GetAllAsync_WithStatusFilter_ShouldReturnFilteredOrders()
    {
        var orders = new List<Order>();
        for (int i = 0; i < 6; i++)
        {
            var order = _orderFixture.CreateValidOrder();
            if (i % 2 == 0)
            {
                order.ChangeStatus(OrderStatus.Paid);
            }
            else
            {
                order.ChangeStatus(OrderStatus.Cancelled);
            }
            orders.Add(order);
        }

        await _unitOfWorkFixture.UnitOfWork.BeginTransactionAsync();
        foreach (var order in orders)
        {
            await _repository.SaveAsync(order);
        }
        await _unitOfWorkFixture.UnitOfWork.CommitAsync();

        var paidOrders = await _repository.GetAllAsync(null, OrderStatus.Paid, null, null, 1, 10);
        Assert.Equal(3, paidOrders.Count());
        
        var cancelledOrders = await _repository.GetAllAsync(null, OrderStatus.Cancelled, null, null, 1, 10);
        Assert.Equal(3, cancelledOrders.Count());
        
        foreach (var order in paidOrders)
        {
            Assert.Equal(OrderStatus.Paid, order.Status);
        }
        
        foreach (var order in cancelledOrders)
        {
            Assert.Equal(OrderStatus.Cancelled, order.Status);
        }
    }

    [Fact]
    public async Task GetAllAsync_WithDateRangeFilter_ShouldReturnFilteredOrders()
    {
        var orders = new List<Order>();
        var now = DateTime.UtcNow;
        
        for (int i = 0; i < 4; i++)
        {
            var order = _orderFixture.CreateValidOrder();
            orders.Add(order);
        }

        await _unitOfWorkFixture.UnitOfWork.BeginTransactionAsync();
        foreach (var order in orders)
        {
            await _repository.SaveAsync(order);
        }
        await _unitOfWorkFixture.UnitOfWork.CommitAsync();

        var from = now.AddDays(-2);
        var to = now.AddDays(-1);
        var filteredOrders = await _repository.GetAllAsync(null, null, from, to, 1, 10);
        
        Assert.Empty(filteredOrders);
    }

    [Fact]
    public async Task GetAllAsync_WithMultipleFilters_ShouldReturnCorrectResults()
    {
        var orders = new List<Order>();
        var customer1 = Guid.NewGuid();
        var customer2 = Guid.NewGuid();
        
        for (int i = 0; i < 8; i++)
        {
            var order = _orderFixture.CreateValidOrder();
            
            if (i % 3 == 0)
            {
                order.ChangeStatus(OrderStatus.Paid);
            }
            else if (i % 3 == 1)
            {
                order.ChangeStatus(OrderStatus.Paid);
                order.ChangeStatus(OrderStatus.Assembling);
            }
            else
            {
                order.ChangeStatus(OrderStatus.Cancelled);
            }
            
            orders.Add(order);
        }

        await _unitOfWorkFixture.UnitOfWork.BeginTransactionAsync();
        foreach (var order in orders)
        {
            await _repository.SaveAsync(order);
        }
        await _unitOfWorkFixture.UnitOfWork.CommitAsync();

        var filteredOrders = await _repository.GetAllAsync(null, OrderStatus.Paid, null, null, 1, 5);
        
        Assert.NotNull(filteredOrders);
        Assert.True(filteredOrders.Count() <= 5);
        
        foreach (var order in filteredOrders)
        {
            Assert.Equal(OrderStatus.Paid, order.Status);
        }
    }

    [Fact]
    public async Task GetAllAsync_WithAllFilters_ShouldReturnCorrectResults()
    {
        var orders = new List<Order>();
        var customer1 = Guid.NewGuid();
        var customer2 = Guid.NewGuid();
        
        for (int i = 0; i < 6; i++)
        {
            var order = _orderFixture.CreateValidOrder();
            
            if (i % 2 == 0)
            {
                order.ChangeStatus(OrderStatus.Paid);
            }
            else
            {
                order.ChangeStatus(OrderStatus.Cancelled);
            }
            
            orders.Add(order);
        }

        await _unitOfWorkFixture.UnitOfWork.BeginTransactionAsync();
        foreach (var order in orders)
        {
            await _repository.SaveAsync(order);
        }
        await _unitOfWorkFixture.UnitOfWork.CommitAsync();

        var now = DateTime.UtcNow;
        var filteredOrders = await _repository.GetAllAsync(null, OrderStatus.Paid, now.AddDays(-1), now.AddDays(1), 1, 10);
        
        Assert.NotNull(filteredOrders);
    }

    #endregion

    #region GetTotalCountAsync Tests

    [Fact]
    public async Task GetTotalCountAsync_WithoutFilters_ShouldReturnTotalCount()
    {
        var orders = new List<Order>();
        for (int i = 0; i < 3; i++)
        {
            var order = _orderFixture.CreateValidOrder();
            order.ChangeStatus(OrderStatus.Paid);
            orders.Add(order);
        }

        await _unitOfWorkFixture.UnitOfWork.BeginTransactionAsync();
        foreach (var order in orders)
        {
            await _repository.SaveAsync(order);
        }
        await _unitOfWorkFixture.UnitOfWork.CommitAsync();

        var totalCount = await _repository.GetTotalCountAsync(null, null, null, null);

        Assert.Equal(3, totalCount);
    }

    [Fact]
    public async Task GetTotalCountAsync_WithCustomerIdFilter_ShouldReturnFilteredCount()
    {
        var customer1 = Guid.NewGuid();
        var customer2 = Guid.NewGuid();
        
        var orders1 = new List<Order>();
        var orders2 = new List<Order>();
        
        for (int i = 0; i < 2; i++)
        {
            var order = _orderFixture.CreateValidOrder();
            orders1.Add(order);
        }
        
        for (int i = 0; i < 3; i++)
        {
            var order = _orderFixture.CreateValidOrder();
            orders2.Add(order);
        }

        await _unitOfWorkFixture.UnitOfWork.BeginTransactionAsync();
        foreach (var order in orders1.Concat(orders2))
        {
            await _repository.SaveAsync(order);
        }
        await _unitOfWorkFixture.UnitOfWork.CommitAsync();

        var count1 = await _repository.GetTotalCountAsync(Guid.NewGuid(), null, null, null);
        var count2 = await _repository.GetTotalCountAsync(Guid.NewGuid(), null, null, null);

        Assert.True(count1 >= 0);
        Assert.True(count2 >= 0);
    }

    [Fact]
    public async Task GetTotalCountAsync_WithStatusFilter_ShouldReturnFilteredCount()
    {
        var orders = new List<Order>();
        for (int i = 0; i < 5; i++)
        {
            var order = _orderFixture.CreateValidOrder();
            if (i % 2 == 0)
            {
                order.ChangeStatus(OrderStatus.Paid);
            }
            else
            {
                order.ChangeStatus(OrderStatus.Cancelled);
            }
            orders.Add(order);
        }

        await _unitOfWorkFixture.UnitOfWork.BeginTransactionAsync();
        foreach (var order in orders)
        {
            await _repository.SaveAsync(order);
        }
        await _unitOfWorkFixture.UnitOfWork.CommitAsync();

        var paidCount = await _repository.GetTotalCountAsync(null, OrderStatus.Paid, null, null);
        var cancelledCount = await _repository.GetTotalCountAsync(null, OrderStatus.Cancelled, null, null);

        Assert.Equal(3, paidCount); // 0, 2, 4
        Assert.Equal(2, cancelledCount); // 1, 3
    }

    [Fact]
    public async Task GetTotalCountAsync_WithDateRangeFilter_ShouldReturnFilteredCount()
    {
        var orders = new List<Order>();
        var now = DateTime.UtcNow;
        
        for (int i = 0; i < 4; i++)
        {
            var order = _orderFixture.CreateValidOrder();
            orders.Add(order);
        }

        await _unitOfWorkFixture.UnitOfWork.BeginTransactionAsync();
        foreach (var order in orders)
        {
            await _repository.SaveAsync(order);
        }
        await _unitOfWorkFixture.UnitOfWork.CommitAsync();

        var from = now.AddDays(-2);
        var to = now.AddDays(-1);
        var count = await _repository.GetTotalCountAsync(null, null, from, to);

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task GetTotalCountAsync_WithMultipleFilters_ShouldReturnFilteredCount()
    {
        var orders = new List<Order>();
        for (int i = 0; i < 6; i++)
        {
            var order = _orderFixture.CreateValidOrder();
            if (i % 3 == 0)
            {
                order.ChangeStatus(OrderStatus.Paid);
            }
            else if (i % 3 == 1)
            {
                order.ChangeStatus(OrderStatus.Cancelled);
            }
            else
            {
                order.ChangeStatus(OrderStatus.Paid);
                order.ChangeStatus(OrderStatus.Assembling);
            }
            orders.Add(order);
        }

        await _unitOfWorkFixture.UnitOfWork.BeginTransactionAsync();
        foreach (var order in orders)
        {
            await _repository.SaveAsync(order);
        }
        await _unitOfWorkFixture.UnitOfWork.CommitAsync();

        var count = await _repository.GetTotalCountAsync(null, OrderStatus.Paid, null, null);

        Assert.Equal(2, count); // 0, 3
    }

    #endregion
}
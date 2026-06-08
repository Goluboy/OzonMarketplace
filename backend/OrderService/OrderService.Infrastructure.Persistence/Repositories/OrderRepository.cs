using Dapper;
using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces.Persistence;
using OrderService.Domain.ValueObjects;
using System.Data;

namespace OrderService.Infrastructure.Persistence.Repositories;

public class OrderRepository(IDbConnection connection, IUnitOfWork unitOfWork) : IOrderRepository
{
    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT o.Id, o.CustomerId, o.CustomerName, o.CustomerEmail, o.DeliveryAddress, 
                   o.Status, o.TotalAmount, o.CreatedAt, o.UpdatedAt, o.CancelledAt, o.Version,
                   i.Id AS ItemId, i.ProductId, i.ProductName, i.Quantity, i.PriceAtPurchase, 
                   i.Subtotal, i.CreatedAt AS ItemCreatedAt, i.UpdatedAt AS ItemUpdatedAt
            FROM Orders o
            LEFT JOIN OrderItems i ON o.Id = i.OrderId
            WHERE o.Id = @Id
            ORDER BY o.Id, i.CreatedAt;";

        var rows = await connection.QueryAsync(sql, new { Id = id }, transaction: unitOfWork.CurrentTransaction);

        if (!rows.Any())
            return null;

        return RehydrateOrderAggregate(rows);
    }

    private static Order RehydrateOrderAggregate(IEnumerable<dynamic> rows)
    {
        var rowList = rows.ToList();
        var firstRow = rowList.First();

        var orderId = new OrderId((Guid)firstRow.Id);
        var customerId = (Guid)firstRow.CustomerId;
        var customerName = (string)firstRow.CustomerName;
        var customerEmail = new Email((string)firstRow.CustomerEmail);
        var deliveryAddress = firstRow.DeliveryAddress != null 
            ? DeliveryAddress.Create((string)firstRow.DeliveryAddress) 
            : null;
        var status = (OrderStatus)(int)firstRow.Status;
        var totalAmount = new Money((decimal)firstRow.TotalAmount);
        var createdAt = (DateTime)firstRow.CreatedAt;
        var updatedAt = firstRow.UpdatedAt != DBNull.Value ? (DateTime?)firstRow.UpdatedAt : null;
        var cancelledAt = firstRow.CancelledAt != DBNull.Value ? (DateTime?)firstRow.CancelledAt : null;
        var version = (int)firstRow.Version;

        var items = new List<OrderItem>();
        foreach (var row in rowList)
        {
            if (row.ItemId == DBNull.Value || row.ItemId == null)
                continue;

            var item = OrderItem.Rehydrate(
                id: (Guid)row.ItemId,
                orderId: orderId,
                productId: (Guid)row.ProductId,
                productName: (string)row.ProductName,
                quantity: (int)row.Quantity,
                priceAtPurchase: new Money((decimal)row.PriceAtPurchase),
                subtotal: new Money((decimal)row.Subtotal),
                createdAt: (DateTime)row.ItemCreatedAt,
                updatedAt: row.ItemUpdatedAt != DBNull.Value ? (DateTime?)row.ItemUpdatedAt : null);

            items.Add(item);
        }

        return Order.Rehydrate(
            orderId,
            customerId,
            customerName,
            customerEmail,
            deliveryAddress,
            status,
            totalAmount,
            createdAt,
            updatedAt,
            cancelledAt,
            version,
            items);
    }

    public async Task SaveAsync(Order order, CancellationToken cancellationToken = default)
    {
        const string updateOrderSql = @"
            INSERT INTO Orders (Id, CustomerId, CustomerName, CustomerEmail, DeliveryAddress, Status, TotalAmount, CreatedAt, UpdatedAt, CancelledAt, Version)
            VALUES (@Id, @CustomerId, @CustomerName, @CustomerEmail, @DeliveryAddress, @Status, @TotalAmount, @CreatedAt, @UpdatedAt, @CancelledAt, @Version)
            ON CONFLICT (Id) DO UPDATE SET
                CustomerName = EXCLUDED.CustomerName,
                CustomerEmail = EXCLUDED.CustomerEmail,
                DeliveryAddress = EXCLUDED.DeliveryAddress,
                Status = EXCLUDED.Status,
                TotalAmount = EXCLUDED.TotalAmount,
                UpdatedAt = EXCLUDED.UpdatedAt,
                CancelledAt = EXCLUDED.CancelledAt,
                Version = EXCLUDED.Version;";
        
        await connection.ExecuteAsync(updateOrderSql, new
        {
            Id = order.Id.Value,
            order.CustomerId,
            order.CustomerName,
            CustomerEmail = order.CustomerEmail.Value,
            DeliveryAddress = order.DeliveryAddress?.AddressLine,
            Status = (int)order.Status,
            TotalAmount = order.TotalAmount.Value,
            order.CreatedAt,
            order.UpdatedAt,
            order.CancelledAt,
            order.Version
        }, transaction: unitOfWork.CurrentTransaction);

        await connection.ExecuteAsync("DELETE FROM OrderItems WHERE OrderId = @Id", new { Id = order.Id.Value }, transaction: unitOfWork.CurrentTransaction);

        const string insertItemSql = @"
            INSERT INTO OrderItems (Id, OrderId, ProductId, ProductName, Quantity, PriceAtPurchase, Subtotal, CreatedAt, UpdatedAt)
            VALUES (@Id, @OrderId, @ProductId, @ProductName, @Quantity, @PriceAtPurchase, @Subtotal, @CreatedAt, @UpdatedAt);";

        foreach (var item in order.Items)
        {
            await connection.ExecuteAsync(insertItemSql, new
            {
                item.Id,
                OrderId = order.Id.Value,
                item.ProductId,
                item.ProductName,
                item.Quantity,
                PriceAtPurchase = item.PriceAtPurchase.Value,
                Subtotal = item.Subtotal.Value,
                item.CreatedAt,
                item.UpdatedAt
            }, transaction: unitOfWork.CurrentTransaction);
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await connection.ExecuteAsync("DELETE FROM Orders WHERE Id = @Id", new { Id = id }, transaction: unitOfWork.CurrentTransaction);
    }
}

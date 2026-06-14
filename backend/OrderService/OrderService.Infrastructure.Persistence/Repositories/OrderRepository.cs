using Dapper;
using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces.Persistence;
using OrderService.Domain.ValueObjects;
using OrderService.Infrastructure.Persistence.UnitOfWork;
using System.Data;

namespace OrderService.Infrastructure.Persistence.Repositories;

public class OrderRepository(IDbSession dbSession) : IOrderRepository
{
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
        var updatedAt = firstRow.UpdatedAt is not null ? (DateTime?)firstRow.UpdatedAt : null;
        var cancelledAt = firstRow.CancelledAt is not null ? (DateTime?)firstRow.CancelledAt : null;
        var version = (int)firstRow.Version;

        var items = new List<OrderItem>();
        foreach (var row in rowList)
        {
            if (row.ItemId == null || row.ItemId is DBNull)
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
                updatedAt: row.ItemUpdatedAt is not null ? (DateTime?)row.ItemUpdatedAt : null);

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

    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           SELECT o."Id", o."CustomerId", o."CustomerName", o."CustomerEmail", o."DeliveryAddress",
                                  o."Status", o."TotalAmount", o."CreatedAt", o."UpdatedAt", o."CancelledAt", o."Version",
                                  i."Id" AS "ItemId", i."ProductId", i."ProductName", i."Quantity", i."PriceAtPurchase",
                                  i."Subtotal", i."CreatedAt" AS "ItemCreatedAt", i."UpdatedAt" AS "ItemUpdatedAt"
                           FROM "Orders" o
                           LEFT JOIN "OrderItems" i ON o."Id" = i."OrderId"
                           WHERE o."Id" = @Id
                           ORDER BY o."Id", i."CreatedAt"
                           """;

        var rows = await dbSession.Connection.QueryAsync(sql, new { Id = id }, transaction: dbSession.Transaction);

        if (!rows.Any())
            return null;

        return RehydrateOrderAggregate(rows);
    }

    public async Task<IEnumerable<Order>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           SELECT o."Id", o."CustomerId", o."CustomerName", o."CustomerEmail", o."DeliveryAddress",
                                  o."Status", o."TotalAmount", o."CreatedAt", o."UpdatedAt", o."CancelledAt", o."Version",
                                  i."Id" AS "ItemId", i."ProductId", i."ProductName", i."Quantity", i."PriceAtPurchase",
                                  i."Subtotal", i."CreatedAt" AS "ItemCreatedAt", i."UpdatedAt" AS "ItemUpdatedAt"
                           FROM "Orders" o
                           LEFT JOIN "OrderItems" i ON o."Id" = i."OrderId"
                           WHERE o."CustomerId" = @CustomerId
                           ORDER BY o."Id", i."CreatedAt"
                           """;

        var rows = await dbSession.Connection.QueryAsync(sql, new { CustomerId = customerId }, transaction: dbSession.Transaction);

        var groupedRows = rows.GroupBy(r => r.Id);
        var orders = new List<Order>();

        foreach (var group in groupedRows)
        {
            orders.Add(RehydrateOrderAggregate(group));
        }

        return orders;
    }

    public async Task<(IEnumerable<Order> Orders, int TotalCount)> GetAllAsync(
    int page, int pageSize, CancellationToken cancellationToken = default)
    {
        const string countSql = "SELECT COUNT(*) FROM \"Orders\"";
        var totalCount = await dbSession.Connection.QuerySingleAsync<int>(
            countSql, transaction: dbSession.Transaction);

        const string sql = """
                       SELECT o."Id", o."CustomerId", o."CustomerName", o."CustomerEmail", o."DeliveryAddress",
                              o."Status", o."TotalAmount", o."CreatedAt", o."UpdatedAt", o."CancelledAt", o."Version",
                              i."Id" AS "ItemId", i."ProductId", i."ProductName", i."Quantity", i."PriceAtPurchase",
                              i."Subtotal", i."CreatedAt" AS "ItemCreatedAt", i."UpdatedAt" AS "ItemUpdatedAt"
                       FROM "Orders" o
                       INNER JOIN (
                           SELECT "Id" 
                           FROM "Orders"
                           ORDER BY "CreatedAt" DESC
                           OFFSET @Offset ROWS
                           FETCH NEXT @PageSize ROWS ONLY
                       ) paged ON o."Id" = paged."Id"
                       LEFT JOIN "OrderItems" i ON o."Id" = i."OrderId"
                       ORDER BY o."CreatedAt" DESC
                       """;

        var rows = await dbSession.Connection.QueryAsync(
            sql,
            new { Offset = (page - 1) * pageSize, PageSize = pageSize },
            transaction: dbSession.Transaction);

        var groupedRows = rows.GroupBy(r => (Guid)r.Id);
        var orders = new List<Order>();

        foreach (var group in groupedRows)
        {
            orders.Add(RehydrateOrderAggregate(group));
        }

        return (orders, totalCount);
    }

    public async Task<IEnumerable<Order>> GetAllAsync(
    Guid? customerId,
    OrderStatus? status,
    DateTime? dateFrom,
    DateTime? dateTo,
    int page,
    int pageSize,
    CancellationToken cancellationToken = default)
    {
        var conditions = new List<string>();
        var parameters = new DynamicParameters();

        if (customerId.HasValue)
        {
            conditions.Add("\"CustomerId\" = @CustomerId");
            parameters.Add("@CustomerId", customerId.Value);
        }

        if (status.HasValue)
        {
            conditions.Add("\"Status\" = @Status");
            parameters.Add("@Status", (int)status.Value);
        }

        if (dateFrom.HasValue)
        {
            conditions.Add("\"CreatedAt\" >= @DateFrom");
            parameters.Add("@DateFrom", dateFrom.Value);
        }

        if (dateTo.HasValue)
        {
            conditions.Add("\"CreatedAt\" <= @DateTo"); 
            parameters.Add("@DateTo", dateTo.Value);
        }

        var whereClause = conditions.Any() ? "WHERE " + string.Join(" AND ", conditions) : "";
        var orderByClause = "ORDER BY \"CreatedAt\" DESC";

        const string sql = """
                       SELECT o."Id", o."CustomerId", o."CustomerName", o."CustomerEmail", o."DeliveryAddress",
                              o."Status", o."TotalAmount", o."CreatedAt", o."UpdatedAt", o."CancelledAt", o."Version",
                              i."Id" AS "ItemId", i."ProductId", i."ProductName", i."Quantity", i."PriceAtPurchase",
                              i."Subtotal", i."CreatedAt" AS "ItemCreatedAt", i."UpdatedAt" AS "ItemUpdatedAt"
                       FROM "Orders" o
                       INNER JOIN (
                           SELECT "Id", "CreatedAt"
                           FROM "Orders"
                           {WhereClause}
                           {OrderByClause}
                           OFFSET @Offset ROWS
                           FETCH NEXT @PageSize ROWS ONLY
                       ) paged ON o."Id" = paged."Id"
                       LEFT JOIN "OrderItems" i ON o."Id" = i."OrderId"
                       ORDER BY o."CreatedAt" DESC
                       """;

        var fullSql = sql
            .Replace("{WhereClause}", whereClause)
            .Replace("{OrderByClause}", orderByClause);

        parameters.Add("@Offset", (page - 1) * pageSize);
        parameters.Add("@PageSize", pageSize);

        var rows = await dbSession.Connection.QueryAsync(fullSql, parameters, transaction: dbSession.Transaction);

        var groupedRows = rows.GroupBy(r => (Guid)r.Id);
        var orders = new List<Order>();

        foreach (var group in groupedRows)
        {
            orders.Add(RehydrateOrderAggregate(group));
        }

        return orders;
    }

    public async Task<Order?> GetByIdForAdminAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           SELECT o."Id", o."CustomerId", o."CustomerName", o."CustomerEmail", o."DeliveryAddress",
                                  o."Status", o."TotalAmount", o."CreatedAt", o."UpdatedAt", o."CancelledAt", o."Version",
                                  i."Id" AS "ItemId", i."ProductId", i."ProductName", i."Quantity", i."PriceAtPurchase",
                                  i."Subtotal", i."CreatedAt" AS "ItemCreatedAt", i."UpdatedAt" AS "ItemUpdatedAt"
                           FROM "Orders" o
                           LEFT JOIN "OrderItems" i ON o."Id" = i."OrderId"
                           WHERE o."Id" = @Id
                           ORDER BY o."Id", i."CreatedAt"
                           """;

        var rows = await dbSession.Connection.QueryAsync(sql, new { Id = id }, transaction: dbSession.Transaction);

        if (!rows.Any())
            return null;

        return RehydrateOrderAggregate(rows);
    }

    public async Task SaveAsync(Order order, CancellationToken cancellationToken = default)
    {
        const string updateOrderSql = """
            INSERT INTO "Orders" ("Id", "CustomerId", "CustomerName", "CustomerEmail", "DeliveryAddress", "Status", "TotalAmount", "CreatedAt", "UpdatedAt", "CancelledAt", "Version")
            VALUES (@Id, @CustomerId, @CustomerName, @CustomerEmail, @DeliveryAddress, @Status, @TotalAmount, @CreatedAt, @UpdatedAt, @CancelledAt, @Version)
            ON CONFLICT ("Id") DO UPDATE SET
                "CustomerName" = EXCLUDED."CustomerName",
                "CustomerEmail" = EXCLUDED."CustomerEmail",
                "DeliveryAddress" = EXCLUDED."DeliveryAddress",
                "Status" = EXCLUDED."Status",
                "TotalAmount" = EXCLUDED."TotalAmount",
                "UpdatedAt" = EXCLUDED."UpdatedAt",
                "CancelledAt" = EXCLUDED."CancelledAt",
                "Version" = EXCLUDED."Version"
            """;

        await dbSession.Connection.ExecuteAsync(updateOrderSql, new
        {
            Id = order.Id.Value,
            order.CustomerId,
            order.CustomerName,
            CustomerEmail = order.CustomerEmail.Value,
            DeliveryAddress = order.DeliveryAddress?.AddressLine,
            Status = (int)order.Status,
            TotalAmount = order.TotalAmount.Amount,
            order.CreatedAt,
            order.UpdatedAt,
            order.CancelledAt,
            order.Version
        }, transaction: dbSession.Transaction);

        await dbSession.Connection.ExecuteAsync("DELETE FROM \"OrderItems\" WHERE \"OrderId\" = @Id", new { Id = order.Id.Value }, transaction: dbSession.Transaction);

        const string insertItemSql = """
            INSERT INTO "OrderItems" ("Id", "OrderId", "ProductId", "ProductName", "Quantity", "PriceAtPurchase", "Subtotal", "CreatedAt", "UpdatedAt")
            VALUES (@Id, @OrderId, @ProductId, @ProductName, @Quantity, @PriceAtPurchase, @Subtotal, @CreatedAt, @UpdatedAt)
            """;

        var itemsParams = order.Items.Select(item => new
        {
            item.Id,
            OrderId = order.Id.Value,
            item.ProductId,
            item.ProductName,
            item.Quantity,
            PriceAtPurchase = item.PriceAtPurchase.Amount,
            Subtotal = item.Subtotal.Amount,
            item.CreatedAt,
            item.UpdatedAt
        });

        await dbSession.Connection.ExecuteAsync(insertItemSql, itemsParams, transaction: dbSession.Transaction);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await dbSession.Connection.ExecuteAsync("DELETE FROM \"Orders\" WHERE \"Id\" = @Id", new { Id = id }, transaction: dbSession.Transaction);
    }

    public async Task<int> GetTotalCountAsync(Guid? customerId, OrderStatus? status, DateTime? dateFrom, DateTime? dateTo, CancellationToken cancellationToken = default)
    {
        var conditions = new List<string>();
        var parameters = new DynamicParameters();

        if (customerId.HasValue)
        {
            conditions.Add("o.\"CustomerId\" = @CustomerId");
            parameters.Add("@CustomerId", customerId.Value);
        }

        if (status.HasValue)
        {
            conditions.Add("o.\"Status\" = @Status");
            parameters.Add("@Status", (int)status.Value);
        }

        if (dateFrom.HasValue)
        {
            conditions.Add("o.\"CreatedAt\" >= @DateFrom");
            parameters.Add("@DateFrom", dateFrom.Value);
        }

        if (dateTo.HasValue)
        {
            conditions.Add("o.\"CreatedAt\" <= @DateTo");
            parameters.Add("@DateTo", dateTo.Value);
        }

        var whereClause = conditions.Any() ? "WHERE " + string.Join(" AND ", conditions) : "";

        const string sql = "SELECT COUNT(*) FROM \"Orders\" o {WhereClause}";

        var fullSql = sql.Replace("{WhereClause}", whereClause);

        return await dbSession.Connection.QuerySingleAsync<int>(fullSql, parameters, transaction: dbSession.Transaction);
    }
}

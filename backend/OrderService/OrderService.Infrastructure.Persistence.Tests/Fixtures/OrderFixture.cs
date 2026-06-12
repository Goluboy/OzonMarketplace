using OrderService.Domain.Entities;
using OrderService.Domain.ValueObjects;

namespace OrderService.Infrastructure.Persistence.Tests.Fixtures;

public class OrderFixture
{
    public Guid CustomerId { get; } = Guid.NewGuid();
    public string CustomerName { get; } = "Ff Fff";
    public string CustomerEmail { get; } = "ffF@ffF.com";
    public string DeliveryAddress { get; } = "екб такая-то такая-то улица такой-то такой-то дом";

    private OrderStatus[] _allStatuses = new[]
    {
        OrderStatus.Created,
        OrderStatus.Paid,
        OrderStatus.Assembling,
        OrderStatus.Shipping,
        OrderStatus.Delivered
    };

    public OrderItem CreateOrderItem(
        Guid? productId = null,
        string productName = "Test Product",
        int quantity = 2,
        decimal price = 25.00m)
    {
        return OrderItem.Create(
            productId ?? Guid.NewGuid(),
            productName,
            quantity,
            price);
    }

    public List<OrderItem> CreateOrderItems(int count = 3)
    {
        var items = new List<OrderItem>();
        for (int i = 0; i < count; i++)
        {
            items.Add(CreateOrderItem(
                productName: $"Product {i + 1}",
                quantity: i + 1,
                price: 10.00m * (i + 1)));
        }
        return items;
    }

    public Order CreateValidOrder(IEnumerable<OrderItem>? items = null)
    {
        return Order.Create(
            CustomerId,
            CustomerName,
            CustomerEmail,
            DeliveryAddress,
            items ?? CreateOrderItems());
    }

    public void ChangeOrderStatus(Order order, OrderStatus targetStatus)
    {
        var statusPath = GetStatusPath(order.Status, targetStatus);
        foreach (var status in statusPath)
        {
            if (order.Status != status)
            {
                order.ChangeStatus(status);
            }
        }
    }

    public void ForceSetStatus(Order order, OrderStatus targetStatus)
    {
        if (order.Status == targetStatus)
        {
            return;
        }

        var path = GetStatusPath(order.Status, targetStatus);
        foreach (var status in path)
        {
            if (order.Status != status)
            {
                try
                {
                    order.ChangeStatus(status);
                }
                catch
                {
                    // Ignore invalid transitions
                }
            }
        }
    }

    public IEnumerable<OrderStatus> GetStatusPath(OrderStatus from, OrderStatus to)
    {
        var fromIndex = Array.IndexOf(_allStatuses, from);
        var toIndex = Array.IndexOf(_allStatuses, to);

        if (fromIndex <= toIndex)
        {
            for (int i = fromIndex + 1; i <= toIndex; i++)
            {
                yield return _allStatuses[i];
            }
        }
        else
        {
            yield return OrderStatus.Cancelled;
        }
    }
}
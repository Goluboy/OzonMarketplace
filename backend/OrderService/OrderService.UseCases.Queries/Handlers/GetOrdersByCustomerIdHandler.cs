using OrderService.Domain.Interfaces.Persistence;
using OrderService.UseCases.Queries.Interfaces;
using OrderService.UseCases.Queries.Models;
using OrderService.UseCases.Queries.Queries;

namespace OrderService.UseCases.Queries.Handlers;

public class GetOrdersByCustomerIdHandler(IOrderRepository orderRepository) : IQueryHandler<GetOrdersByCustomerIdQuery, List<OrderModel>>
{
    public async Task<List<OrderModel>> HandleAsync(GetOrdersByCustomerIdQuery query, CancellationToken ct = default)
    {
        var orders = await orderRepository.GetByCustomerIdAsync(query.CustomerId, ct);

        return orders.Select(order => new OrderModel(
            Id: order.Id.Value,
            CustomerId: order.CustomerId,
            CustomerName: order.CustomerName,
            CustomerEmail: order.CustomerEmail.Value,
            DeliveryAddress: order.DeliveryAddress?.AddressLine,
            Status: order.Status.ToString(),
            TotalAmount: order.TotalAmount.Value,
            CreatedAt: order.CreatedAt,
            UpdatedAt: order.UpdatedAt,
            Items: order.Items.Select(item => new OrderItemModel(
                item.Id,
                item.ProductId,
                item.ProductName,
                item.Quantity,
                item.PriceAtPurchase.Value,
                item.Subtotal.Value
            )).ToList()
        )).ToList();
    }
}

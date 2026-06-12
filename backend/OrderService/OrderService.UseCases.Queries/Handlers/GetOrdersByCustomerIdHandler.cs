using OrderService.Domain.Interfaces.Persistence;
using OrderService.UseCases.Queries.Interfaces;
using OrderService.UseCases.Queries.Models;

namespace OrderService.UseCases.Queries.Handlers;

public class GetOrdersByCustomerIdHandler(IOrderRepository orderRepository) : IQueryHandler<GetOrdersByCustomerIdQuery, List<OrderModel>>
{

    public async Task<List<OrderModel>> HandleAsync(GetOrdersByCustomerIdQuery query, CancellationToken cancellationToken = default)
    {

        var orders = await orderRepository.GetByCustomerIdAsync(query.CustomerId, cancellationToken);

        return orders.Select(order => new OrderModel(
            order.Id.Value,
            order.CustomerId,
            order.CustomerName,
            order.CustomerEmail.Value,
            order.DeliveryAddress?.AddressLine,
            order.Status,
            order.TotalAmount.Amount,
            order.TotalAmount.Currency,
            order.CreatedAt,
            order.UpdatedAt,
            Items: order.Items.Select(item => new OrderItemModel(
                item.Id,
                item.ProductId,
                item.ProductName,
                item.Quantity,
                item.PriceAtPurchase.Amount,
                item.PriceAtPurchase.Currency,
                item.Subtotal.Amount
            )).ToList()
        )).ToList();
    }
}

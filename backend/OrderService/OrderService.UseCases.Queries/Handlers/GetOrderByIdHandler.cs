using OrderService.Domain.Interfaces.Persistence;
using OrderService.UseCases.Queries.Interfaces;
using OrderService.UseCases.Queries.Models;

namespace OrderService.UseCases.Queries.Handlers;

public class GetOrderByIdHandler(IOrderRepository orderRepository) : IQueryHandler<GetOrderByIdQuery, OrderModel?>
{
    public async Task<OrderModel?> HandleAsync(GetOrderByIdQuery query, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(query.OrderId, cancellationToken);

        if (order == null)
        {
            return null;
        }

        return new OrderModel(
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
            order.Items.Select(item => new OrderItemModel(
                item.Id,
                item.ProductId,
                item.ProductName,
                item.Quantity,
                item.PriceAtPurchase.Amount,
                item.Subtotal.Currency,
                item.Subtotal.Amount
            )).ToList()
        );
    }
}

using OrderService.Domain.Interfaces.Persistence;
using OrderService.UseCases.Queries.Interfaces;
using OrderService.UseCases.Queries.Models;

namespace OrderService.UseCases.Queries.Handlers
{
    public class GetOrdersQueryHandler : IQueryHandler<GetOrdersQuery, OrderModel[]>
    {
        private readonly IOrderRepository _orderRepository;

        public GetOrdersQueryHandler(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        public async Task<OrderModel[]> HandleAsync(GetOrdersQuery query, CancellationToken cancellationToken)
        {
            var (orders, totalCount) = await _orderRepository.GetAllAsync(query.Page, query.PageSize);

            var orderModels = orders.Select(order => new OrderModel(
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
                    item.PriceAtPurchase.Currency,
                    item.Subtotal.Amount
                )).ToList()
            )).ToArray();

            return orderModels;
        }
    }
}
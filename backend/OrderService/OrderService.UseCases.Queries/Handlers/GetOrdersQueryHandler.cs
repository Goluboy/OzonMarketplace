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
            var (orders, totalCount) = await _orderRepository.GetOrdersAsync(query.Page, query.PageSize);
            
            var orderModels = orders.Select(order => new OrderModel(order.Id, order.Status, order.CreatedAt, order.UpdatedAt, 
                order.CustomerName, order.CustomerEmail, order.DeliveryAddress, order.TotalAmount.Amount, order.TotalAmount.Currency.ToString(),
                order.Items.Select(item => new OrderItemModel(item.ProductId, item.ProductName, item.Quantity, item.Price.Amount, item.Price.Currency.ToString()))
                .ToList())).ToList();

            return orderModels;
        }
    }
}
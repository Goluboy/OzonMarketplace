using OrderService.Domain.Repositories;
using OrderService.UseCases.Queries.Models;

namespace OrderService.UseCases.Queries.Handlers
{
    public class GetOrdersQueryHandler : IQueryHandler<GetOrdersQuery, PagedResult<OrderModel>>
    {
        private readonly IOrderRepository _orderRepository;

        public GetOrdersQueryHandler(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        public async Task<PagedResult<OrderModel>> Handle(GetOrdersQuery query, CancellationToken cancellationToken)
        {
            var (orders, totalCount) = await _orderRepository.GetOrdersAsync(query.Page, query.PageSize);
            
            var orderModels = orders.Select(order => new OrderModel(order.Id, order.Status, order.CreatedAt, order.UpdatedAt, 
                order.CustomerName, order.CustomerEmail, order.DeliveryAddress, order.TotalAmount.Amount, order.TotalAmount.Currency.ToString(),
                order.Items.Select(item => new OrderItemModel(item.ProductId, item.ProductName, item.Quantity, item.Price.Amount, item.Price.Currency.ToString()))
                .ToList())).ToList();

            return new PagedResult<OrderModel>(orderModels, totalCount, query.Page, query.PageSize);
        }
    }
}
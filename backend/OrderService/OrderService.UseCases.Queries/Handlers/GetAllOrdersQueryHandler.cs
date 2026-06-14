using OrderService.Domain.Interfaces.Persistence;
using OrderService.UseCases.Queries.Interfaces;
using OrderService.UseCases.Queries.Models;

namespace OrderService.UseCases.Queries.Handlers
{
    public class GetAllOrdersQueryHandler : IQueryHandler<GetAllOrdersQuery, OrderModel[]>
    {
        private readonly IOrderRepository _orderRepository;

        public GetAllOrdersQueryHandler(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        public async Task<OrderModel[]> HandleAsync(GetAllOrdersQuery query, CancellationToken cancellationToken)
        {
            var orders = await _orderRepository.GetAllAsync(
                query.CustomerId,
                query.Status,
                query.DateFrom,
                query.DateTo,
                query.Page,
                query.PageSize,
                cancellationToken);

            var totalCount = await _orderRepository.GetTotalCountAsync(
                query.CustomerId,
                query.Status,
                query.DateFrom,
                query.DateTo,
                cancellationToken);

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
            )).ToList();

            return orderModels.ToArray();
        }
    }
}
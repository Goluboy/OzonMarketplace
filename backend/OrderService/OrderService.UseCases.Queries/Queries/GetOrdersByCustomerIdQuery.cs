using OrderService.UseCases.Queries.Models;

namespace OrderService.UseCases.Queries
{
    public record GetOrdersByCustomerIdQuery(Guid CustomerId, int Page, int PageSize)
        : IQuery<List<OrderModel>>;
}
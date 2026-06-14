using OrderService.UseCases.Queries.Models;
using OrderService.UseCases.Queries.Queries;

namespace OrderService.UseCases.Queries
{
    public record GetOrdersByCustomerIdQuery(Guid CustomerId, int Page, int PageSize)
        : IQuery<List<OrderModel>>;
}
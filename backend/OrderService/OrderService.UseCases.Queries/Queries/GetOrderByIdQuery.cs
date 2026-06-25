using OrderService.UseCases.Queries.Models;
using OrderService.UseCases.Queries.Queries;

namespace OrderService.UseCases.Queries
{
    public record GetOrderByIdQuery(Guid OrderId) : IQuery<OrderModel?>;
}
using OrderService.UseCases.Queries.Models;
using OrderService.UseCases.Queries.Queries;

namespace OrderService.UseCases.Queries
{
    public record GetOrdersQuery(int Page, int PageSize) : IQuery<OrderModel[]>;
}
using OrderService.UseCases.Queries.Models;

namespace OrderService.UseCases.Queries
{
    public record GetOrdersQuery(int Page, int PageSize) : IQuery<OrderModel[]>;
}
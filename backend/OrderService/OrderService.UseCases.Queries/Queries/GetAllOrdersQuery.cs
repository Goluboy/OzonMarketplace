using OrderService.UseCases.Queries.Models;
using OrderService.UseCases.Queries.Queries;

namespace OrderService.UseCases.Queries
{
    public record GetAllOrdersQuery(
        int Page,
        int PageSize,
        Domain.ValueObjects.OrderStatus? Status,
        Guid? CustomerId,
        DateTime? DateFrom,
        DateTime? DateTo) : IQuery<PagedResult<OrderModel>>;
}
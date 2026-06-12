using OrderService.Domain.ValueObjects;
using OrderService.UseCases.Queries.Interfaces;
using OrderService.UseCases.Queries.Models;

namespace OrderService.UseCases.Queries.Queries;

public record GetOrdersByCustomerIdQuery(
    Guid CustomerId,
    int Page,
    int PageSize,
    OrderStatus? Status) : IQuery<PagedResult<OrderModel>>;

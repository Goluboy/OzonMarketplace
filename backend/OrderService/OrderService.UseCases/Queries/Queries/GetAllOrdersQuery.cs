using OrderService.Domain.ValueObjects;
using OrderService.UseCases.Queries.Interfaces;
using OrderService.UseCases.Queries.Models;

namespace OrderService.UseCases.Queries.Queries;

public record GetAllOrdersQuery(
    int Page,
    int PageSize,
    OrderStatus? Status,
    Guid? CustomerId,
    DateTime? DateFrom,
    DateTime? DateTo) : IQuery<PagedResult<OrderModel>>;
using OrderService.Domain.ValueObjects;
using OrderService.Http.Dtos.Shared;

namespace OrderService.Http.Dtos.Responses;

public record CreateOrderAcceptedResponse(
    Guid OrderId,
    OrderStatus Status,
    string StatusCheckUrl);
using OrderService.Domain.ValueObjects;

namespace OrderService.Http.Dtos.Responses;

public record CreateOrderAcceptedResponse(
    Guid OrderId,
    OrderStatus Status,
    string StatusCheckUrl);
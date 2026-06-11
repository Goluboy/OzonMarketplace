using OrderService.Domain.ValueObjects;

namespace OrderService.Http.Dtos;

public record CreateOrderAcceptedResponse(
    Guid OrderId,
    OrderStatus Status,
    string StatusCheckUrl);
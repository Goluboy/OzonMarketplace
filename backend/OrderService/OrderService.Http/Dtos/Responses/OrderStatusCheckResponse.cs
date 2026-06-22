using OrderService.Domain.ValueObjects;

namespace OrderService.Http.Dtos.Responses;

public record OrderStatusCheckResponse(
    OrderStatus Status,
    string Message,
    DateTime UpdatedAt);
using OrderService.Domain.ValueObjects;
using OrderService.Http.Dtos.Shared;

namespace OrderService.Http.Dtos;

public record OrderStatusCheckResponse(
    OrderStatus Status,
    string Message,
    DateTime UpdatedAt);
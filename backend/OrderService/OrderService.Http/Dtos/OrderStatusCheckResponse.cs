using OrderService.Domain.ValueObjects;

namespace OrderService.Http.Dtos;

public record OrderStatusCheckResponse(
    OrderStatus Status,
    string? Message,
    DateTime? UpdatedAt);
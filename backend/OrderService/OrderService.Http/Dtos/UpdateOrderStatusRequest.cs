using OrderService.Domain.ValueObjects;

namespace OrderService.Http.Dtos;

public record UpdateOrderStatusRequest(
    OrderStatus NewStatus,
    string? Comment = null);

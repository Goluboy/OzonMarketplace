using OrderService.Domain.ValueObjects;

namespace OrderService.Http.Dtos.Requests;

public record UpdateOrderStatusRequest(
    OrderStatus NewStatus,
    string? Comment);
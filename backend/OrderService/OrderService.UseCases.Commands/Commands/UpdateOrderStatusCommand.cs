using OrderService.Domain.ValueObjects;

namespace OrderService.UseCases.Commands.Commands;

public record UpdateOrderStatusCommand(
    Guid OrderId,
    OrderStatus NewStatus,
    Guid ChangedBy,
    string? Comment = null);

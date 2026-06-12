using OrderService.UseCases.Commands.Commands;

namespace OrderService.UseCases.Commands.Commands;

public record UpdateOrderStatusCommand(
    Guid OrderId,
    OrderStatus NewStatus,
    Guid ChangedBy,
    string? Comment = null) : ICommand<bool>;

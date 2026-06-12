using OrderService.UseCases.Commands.Commands;

namespace OrderService.UseCases.Commands.Commands;

public record CancelOrderCommand(Guid OrderId, Guid CustomerId) : ICommand<bool>;

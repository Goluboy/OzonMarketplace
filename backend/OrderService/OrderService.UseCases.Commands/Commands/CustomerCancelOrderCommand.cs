namespace OrderService.UseCases.Commands.Commands;

public record CustomerCancelOrderCommand(Guid OrderId, Guid CustomerId) : ICommand<bool>;

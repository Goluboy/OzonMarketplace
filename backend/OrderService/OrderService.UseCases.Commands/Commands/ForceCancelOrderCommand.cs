namespace OrderService.UseCases.Commands.Commands;

public record ForceCancelOrderCommand(Guid OrderId, string Reason) : ICommand<bool>;
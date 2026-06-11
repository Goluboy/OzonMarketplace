namespace OrderService.UseCases.Commands.Commands;

public record CancelOrderCommand(Guid OrderId, Guid CustomerId);

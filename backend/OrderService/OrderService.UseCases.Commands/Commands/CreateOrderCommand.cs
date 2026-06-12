namespace OrderService.UseCases.Commands.Commands;

public record CreateOrderCommand(
    Guid CustomerId,
    string CustomerName,
    string CustomerEmail,
    string? DeliveryAddress,
    List<CreateOrderCommand.CreateOrderItemCommand> Items) : ICommand<Guid>
{
    public record CreateOrderItemCommand(
        Guid ProductId,
        string ProductName,
        int Quantity,
        decimal Price);
}

namespace OrderService.UseCases.Commands.Interfaces;

public interface ICommandHandler<TCommand, TResponse>
{
    Task<TResponse> HandleAsync(TCommand command, CancellationToken ct = default);
}

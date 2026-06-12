using OrderService.Domain.Interfaces.Persistence;
using OrderService.UseCases.Commands.Commands;
using OrderService.UseCases.Commands.Interfaces;

namespace OrderService.UseCases.Commands.Features.ForceCancelOrder;

public class ForceCancelOrderCommandHandler(
    IOrderRepository orderRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<ForceCancelOrderCommand, bool>
{
    public async Task<bool> HandleAsync(ForceCancelOrderCommand command, CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetByIdForAdminAsync(command.OrderId, cancellationToken);
        if (order is null)
        {
            return false;
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            order.ForceCancel(command.Reason);
            await orderRepository.SaveAsync(order, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
using OrderService.Domain.Interfaces.Persistence;
using OrderService.UseCases.Commands.Commands;
using OrderService.UseCases.Commands.Interfaces;

namespace OrderService.UseCases.Commands.Features.UpdateOrderStatus;

public class UpdateOrderStatusCommandHandler(
    IOrderRepository orderRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdateOrderStatusCommand, bool>
{
    public async Task<bool> HandleAsync(UpdateOrderStatusCommand command, CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetByIdAsync(command.OrderId, cancellationToken);
        if (order is null)
        {
            return false;
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            order.ChangeStatus(command.NewStatus, command.ChangedBy, command.Comment);
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

using OrderService.Domain.Interfaces.Persistence;
using OrderService.Domain.ValueObjects;
using OrderService.UseCases.Commands.Commands;
using OrderService.UseCases.Commands.Interfaces;

namespace OrderService.UseCases.Commands.Features.CancelOrder;

public class CancelOrderCommandHandler(
    IOrderRepository orderRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<CancelOrderCommand, bool>
{
    public async Task<bool> HandleAsync(CancelOrderCommand command, CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetByIdAsync(command.OrderId, cancellationToken);
        if (order is null)
        {
            return false;
        }

        if (!order.IsOwnedBy(command.CustomerId))
        {
            throw new UnauthorizedAccessException("Order belongs to another customer.");
        }

        if (order.Status is OrderStatus.Assembling or OrderStatus.Shipping or OrderStatus.Delivered)
        {
            throw new InvalidOperationException("Order cannot be cancelled in the current status.");
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            return true;
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            order.Cancel(command.CustomerId);
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

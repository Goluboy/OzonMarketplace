using DotNetCore.CAP;
using IntegrationEvents;
using IntegrationEvents.IntegrationEvents;
using Microsoft.Extensions.Logging;
using OrderService.Domain.Interfaces.Persistence;
using OrderService.Domain.ValueObjects;
using OrderService.UseCases.Commands.Commands;
using OrderService.UseCases.Commands.Interfaces;

namespace OrderService.UseCases.Commands.Features.CancelOrder;

public class ForceCancelOrderCommandHandler(
    IOrderRepository orderRepository,
    IUnitOfWork unitOfWork,
    ICapPublisher capPublisher)
    : ICommandHandler<ForceCancelOrderCommand, bool>
{
    public async Task<bool> HandleAsync(ForceCancelOrderCommand command, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(command.OrderId, cancellationToken);

        if (order is null)
        {
            return false;
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            return true;
        }

        await unitOfWork.BeginOutboxTransactionAsync(cancellationToken);

        try
        {
            var itemsToRelease = order.Items
            .Select(i => i.ProductId)
            .ToList();

            order.ForceCancel(command.Reason);

            await orderRepository.SaveAsync(order, cancellationToken);

            await capPublisher.PublishAsync(
                Topics.Orders.OrdersTopic,
                new OrderCancelledEvent
                {
                    CorrelationId = order.Id,
                    Reason = command.Reason,
                    ItemsToRelease = itemsToRelease
                },
                cancellationToken: cancellationToken);

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
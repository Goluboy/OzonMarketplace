using DotNetCore.CAP;
using IntegrationEvents;
using IntegrationEvents.IntegrationEvents;
using Microsoft.Extensions.Logging;
using OrderService.Domain.Events;
using OrderService.Domain.Interfaces.Persistence;
using OrderService.Domain.ValueObjects;
using OrderService.UseCases.Commands.Commands;
using OrderService.UseCases.Commands.Interfaces;

namespace OrderService.UseCases.Commands.Features.UpdateOrderStatus;

public class UpdateOrderStatusCommandHandler(
    IOrderRepository orderRepository,
    IUnitOfWork unitOfWork,
    ICapPublisher capPublisher)
    : ICommandHandler<UpdateOrderStatusCommand, bool>
{
    public async Task<bool> HandleAsync(UpdateOrderStatusCommand command, CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetByIdAsync(command.OrderId, cancellationToken);
        if (order is null)
        {
            return false;
        }

        if (order.Status == command.NewStatus)
        {
            return true;
        }

        var oldStatus = order.Status;

        await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            order.ChangeStatus(command.NewStatus, command.ChangedBy, command.Comment);
            await orderRepository.SaveAsync(order, cancellationToken);

            await capPublisher.PublishAsync(
                Topics.Orders.Created,
                new OrderStatusChangedEvent(order.Id.Value, oldStatus, command.NewStatus, command.ChangedBy, command.Comment, DateTime.UtcNow), cancellationToken: cancellationToken);

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
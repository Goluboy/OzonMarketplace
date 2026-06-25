using DotNetCore.CAP;
using IntegrationEvents;
using IntegrationEvents.IntegrationEvents;
using IntegrationEvents.IntegrationEvents.Order;
using Microsoft.Extensions.Logging;
using OrderService.Domain.Interfaces.Persistence;
using OrderService.Domain.ValueObjects;
using OrderService.UseCases.Commands.Commands;
using OrderService.UseCases.Commands.Interfaces;

namespace OrderService.UseCases.Commands.Features.CancelOrder;

public class CustomerCancelOrderCommandHandler(
    IOrderRepository orderRepository,
    IUnitOfWork unitOfWork,
    ICapPublisher capPublisher)
    : ICommandHandler<CustomerCancelOrderCommand, bool>
{
    public async Task<bool> HandleAsync(CustomerCancelOrderCommand command, CancellationToken cancellationToken = default)
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
            var itemsToRelease = order.Items
                .Select(i => i.ProductId)
                .ToList();

            order.Cancel(command.CustomerId);
            await orderRepository.SaveAsync(order, cancellationToken);

            await capPublisher.PublishAsync(
                Topics.Orders.OrdersTopic,
                new OrderCancelledEvent
                {
                    CorrelationId = order.Id,
                    Reason = $"Cancelled by customer {command.CustomerId}",
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
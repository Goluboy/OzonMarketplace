using Microsoft.Extensions.Logging;
using OrderService.Domain.Interfaces.Persistence;
using OrderService.Domain.ValueObjects;
using OrderService.UseCases.Commands.Commands;
using OrderService.UseCases.Commands.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderService.UseCases.Commands.Features.UpdateStockStatus;

public class UpdateOrderStockStatusCommandHandler(
    IOrderRepository orderRepository,
    IProcessedEventsRepository processedEvents,
    IUnitOfWork unitOfWork,
    ILogger<UpdateOrderStockStatusCommandHandler> logger)
    : ICommandHandler<UpdateOrderStockStatusCommand, bool>
{
    public async Task<bool> HandleAsync(UpdateOrderStockStatusCommand command, CancellationToken ct)
    {
        var order = await orderRepository.GetByIdAsync(command.OrderId, ct);
        if (order is null)
        {
            logger.LogWarning("Order {OrderId} not found for stock status update", command.OrderId);
            return false;
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            logger.LogInformation("Order {OrderId} is already cancelled", command.OrderId);
            return true;
        }

        await unitOfWork.BeginOutboxTransactionAsync(ct);

        try
        {
            order.MarkItemsAsReserved(command.ReservedItems.Select(x => (x.ProductId, x.Quantity)));

            if (order.AllItemsReserved())
            {
                order.MarkAsPaid();
                logger.LogInformation("Order {OrderId} marked as Paid (all items reserved)", command.OrderId);
            }

            await orderRepository.SaveAsync(order, ct);

            if (!string.IsNullOrEmpty(command.MessageId))
            {
                await processedEvents.MarkAsProcessedAsync(
                    command.MessageId,
                    nameof(UpdateOrderStockStatusCommand),
                    ct);
            }

            await unitOfWork.CommitAsync(ct);

            logger.LogInformation(
                "Order {OrderId} stock status updated. Reserved items: {Count}",
                command.OrderId,
                command.ReservedItems.Count);

            return true;
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(ct);
            logger.LogError(ex, "Failed to update stock status for order {OrderId}", command.OrderId);
            throw;
        }
    }
}
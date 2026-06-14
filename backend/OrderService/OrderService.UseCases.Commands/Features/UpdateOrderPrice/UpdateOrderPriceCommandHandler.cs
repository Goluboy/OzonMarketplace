using Microsoft.Extensions.Logging;
using OrderService.Domain.Interfaces.Persistence;
using OrderService.Domain.ValueObjects;
using OrderService.UseCases.Commands.Commands;
using OrderService.UseCases.Commands.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderService.UseCases.Commands.Features.UpdateOrderPrice;

public class UpdateOrderPriceCommandHandler(
    IOrderRepository orderRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateOrderPriceCommand, bool>
{
    public async Task<bool> HandleAsync(UpdateOrderPriceCommand command, CancellationToken ct)
    {
        var order = await orderRepository.GetByIdAsync(command.OrderId, ct);
        if (order is null)
        {
            return false;
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            return true;
        }

        await unitOfWork.BeginOutboxTransactionAsync(ct);

        try
        {
            order.SetTotalAmount(new Money(command.TotalAmount, command.Currency));

            await orderRepository.SaveAsync(order, ct);

            await unitOfWork.CommitAsync(ct);

            return true;
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(ct);
            throw;
        }
    }
}
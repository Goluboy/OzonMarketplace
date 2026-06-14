using DotNetCore.CAP;
using DotNetCore.CAP.Messages;
using IntegrationEvents.IntegrationEvents;
using Microsoft.Extensions.Logging;
using OrderService.Domain.Interfaces.Persistence;
using OrderService.UseCases.Commands.Commands;
using OrderService.UseCases.Commands.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderService.Infrastructure.EventBus.Consumers;

public class StockReservedConsumer(ICommandHandler<UpdateOrderStockStatusCommand, bool> updateStockHandler)
    : BaseConsumer
{
    public async Task HandleAsync(
        StockReservedEvent @event,
        CapHeader header,
        CancellationToken cancellationToken)
    {
        var messageId = header.GetValueOrDefault(Headers.MessageId)
            ?? throw new InvalidOperationException("MessageId is required");

        await ExecuteWithIdempotencyAsync(
            header,
            nameof(StockReservedEvent),
            async () =>
            {
                var command = new UpdateOrderStockStatusCommand(
                    @event.OrderId,
                    @event.ReservedItems,
                    messageId);

                var result = await updateStockHandler.HandleAsync(command, cancellationToken);
            },
            cancellationToken);
    }
}
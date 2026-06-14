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

public class StockReservationFailedConsumer(ICommandHandler<ForceCancelOrderCommand, bool> cancelOrderHandler)
    : BaseConsumer
{
    public async Task HandleAsync(
        StockReservationFailedEvent @event,
        CapHeader header,
        CancellationToken cancellationToken)
    {
        var messageId = header.GetValueOrDefault(Headers.MessageId)
            ?? throw new InvalidOperationException("MessageId is required");

        await ExecuteWithIdempotencyAsync(
            header,
            nameof(StockReservationFailedEvent),
            async () => 
            { 
                var command = new ForceCancelOrderCommand(
                    @event.OrderId,
                    $"Stock reservation failed: {@event.Reason}");

                var result = await cancelOrderHandler.HandleAsync(command, cancellationToken);
            },
            cancellationToken);
    }
}
using DotNetCore.CAP;
using DotNetCore.CAP.Messages;
using IntegrationEvents.IntegrationEvents;
using IntegrationEvents.IntegrationEvents.Order;
using Microsoft.Extensions.Logging;
using OrderService.Domain.Interfaces.Persistence;
using OrderService.UseCases.Commands.Commands;
using OrderService.UseCases.Commands.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderService.Infrastructure.EventBus.Consumers;

public class PriceCalculatedConsumer(ICommandHandler<UpdateOrderPriceCommand, bool> updatePriceHandler)
    : BaseConsumer
{
    public async Task HandleAsync(
        PriceCalculatedEvent @event,
        CapHeader header,
        CancellationToken cancellationToken)
    {
        var messageId = header.GetValueOrDefault(Headers.MessageId)
            ?? throw new InvalidOperationException("MessageId is required");

        await ExecuteWithIdempotencyAsync(
            header,
            nameof(PriceCalculatedEvent),
            async () =>
            {
                var command = new UpdateOrderPriceCommand(
                    @event.OrderId,
                    @event.TotalAmount,
                    @event.Currency,
                    messageId);

                var result = await updatePriceHandler.HandleAsync(command, cancellationToken);
            },
            cancellationToken);
    }
}
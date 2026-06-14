using DotNetCore.CAP;
using DotNetCore.CAP.Messages;
using Microsoft.Extensions.Logging;
using OrderService.Domain.Interfaces.Persistence;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderService.Infrastructure.EventBus.Consumers;

public abstract class BaseConsumer(
    IProcessedEventsRepository processedEvents)
{
    protected async Task ExecuteWithIdempotencyAsync(
        CapHeader header,
        string eventName,
        Func<Task> action,
        CancellationToken cancellationToken)
    {
        var messageId = header.GetValueOrDefault(Headers.MessageId)
            ?? throw new InvalidOperationException("MessageId is required");
        if (await processedEvents.IsProcessedAsync(messageId, cancellationToken))
        {
            return;
        }

        await action();

        await processedEvents.MarkAsProcessedAsync(messageId, eventName, cancellationToken);
    }
}
using DotNetCore.CAP;
using IntegrationEvents.IntegrationEvents;
using OrderService.Domain.Interfaces.Persistence;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderService.Infrastructure.EventBus.Consumers;

public class OrderCancelledConsumer(IProcessedEventsRepository processedEvents) : BaseConsumer(processedEvents)
{
    public async Task HandleAsync(
       OrderCancelledEvent orderCancelled,
       CapHeader header,
       CancellationToken cancellationToken)
    {

    }
}

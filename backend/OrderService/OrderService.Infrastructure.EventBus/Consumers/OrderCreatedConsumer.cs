using DotNetCore.CAP;
using IntegrationEvents.IntegrationEvents;
using IntegrationEvents.Shared;
using OrderService.Domain.Interfaces.Persistence;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderService.Infrastructure.EventBus.Consumers;

public class OrderCreatedConsumer(IProcessedEventsRepository processedEvents) : BaseConsumer(processedEvents)
{
    public async Task HandleAsync(
       OrderCreatedEvent orderSagaTimeout,
       CapHeader header,
       CancellationToken cancellationToken)
    {

    }
}

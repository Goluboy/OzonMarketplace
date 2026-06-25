using DotNetCore.CAP;
using IntegrationEvents.IntegrationEvents;
using IntegrationEvents.IntegrationEvents.Order;
using OrderService.Domain.Interfaces.Persistence;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderService.Infrastructure.EventBus.Consumers;

public class OrderCancelledConsumer : BaseConsumer
{
    public async Task HandleAsync(
       OrderCancelledEvent orderCancelled,
       CapHeader header,
       CancellationToken cancellationToken)
    {

    }
}

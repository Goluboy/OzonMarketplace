using DotNetCore.CAP;
using IntegrationEvents;
using IntegrationEvents.IntegrationEvents;
using IntegrationEvents.IntegrationEvents.Order;
using Microsoft.Extensions.Logging;
using OrderService.Infrastructure.EventBus.Consumers;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderService.Infrastructure.EventBus.Dispatcher;

public class PricesEventDispatcher(
    PriceCalculatedConsumer priceCalculatedConsumer,
    ILogger<PricesEventDispatcher> logger)
    : ICapSubscribe
{
    [CapSubscribe(Topics.Prices.PricesTopic)]
    public async Task HandleAsync(
        IntegrationEvent @event,
        [FromCap] CapHeader header,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Received event: {EventType} with CorrelationId: {CorrelationId}", @event.GetType().Name, @event.CorrelationId);
        switch (@event)
        {
            case PriceCalculatedEvent priceCalculated:
                await priceCalculatedConsumer.HandleAsync(priceCalculated, header, cancellationToken);
                break;
        }
    }
}
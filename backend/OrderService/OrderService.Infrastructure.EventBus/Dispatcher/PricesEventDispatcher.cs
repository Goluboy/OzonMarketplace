using DotNetCore.CAP;
using IntegrationEvents;
using IntegrationEvents.IntegrationEvents;
using OrderService.Infrastructure.EventBus.Consumers;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderService.Infrastructure.EventBus.Dispatcher;

public class PricesEventDispatcher(
    PriceCalculatedConsumer priceCalculatedConsumer)
    : ICapSubscribe
{
    [CapSubscribe(Topics.Prices.PricesTopic)]
    public async Task HandleAsync(
        IntegrationEvent @event,
        [FromCap] CapHeader header,
        CancellationToken cancellationToken)
    {
        switch (@event)
        {
            case PriceCalculatedEvent priceCalculated:
                await priceCalculatedConsumer.HandleAsync(priceCalculated, header, cancellationToken);
                break;
        }
    }
}
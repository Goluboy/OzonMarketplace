using DotNetCore.CAP;
using IntegrationEvents;
using IntegrationEvents.IntegrationEvents;
using OrderService.Infrastructure.EventBus.Consumers;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderService.Infrastructure.EventBus.Dispatcher;

public class ProductsEventDispatcher(
    StockReservedConsumer stockReservedConsumer,
    StockReservationFailedConsumer stockReservationFailedConsumer)
    : ICapSubscribe
{
    [CapSubscribe(Topics.Products.ProductsTopic)]
    public async Task HandleAsync(
        IntegrationEvent @event,
        [FromCap] CapHeader header,
        CancellationToken cancellationToken)
    {
        switch (@event)
        {
            case StockReservedEvent stockReserved:
                await stockReservedConsumer.HandleAsync(stockReserved, header, cancellationToken);
                break;

            case StockReservationFailedEvent stockFailed:
                await stockReservationFailedConsumer.HandleAsync(stockFailed, header, cancellationToken);
                break;
        }
    }
}
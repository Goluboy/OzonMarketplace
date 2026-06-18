using DotNetCore.CAP;
using DotNetCore.CAP.Messages;
using IntegrationEvents;
using IntegrationEvents.IntegrationEvents;
using IntegrationEvents.IntegrationEvents.Order;
using Microsoft.Extensions.Logging;
using ProductService.Application.EventHandlers;
using ProductService.Application.Services.Products.Query;
using ProductService.Infrastructure.Abstractions.EventHandlers.Abstractions;

namespace ProductService.Infrastructure.Saga.Dispatchers;

public class ProductEventDispatcher(IStockReservedEventHandler stockReservedEventHandler)
    : ICapSubscribe
{
    [CapSubscribe(Topics.Products.ProductsTopic)]
    public async Task HandleProductEventAsync(
        IntegrationEvent @event,
        [FromCap] CapHeader header,
        CancellationToken cancellationToken)
    {
        switch (@event)
        {
            case StockReservedEvent stockReserved:
                await stockReservedEventHandler.HandleAsync(stockReserved);
                break;
            default:
                break;
        }
    }
}
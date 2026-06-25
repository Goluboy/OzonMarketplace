using DotNetCore.CAP;
using IntegrationEvents;
using IntegrationEvents.IntegrationEvents;
using IntegrationEvents.IntegrationEvents.Order;
using Microsoft.Extensions.Logging;
using ProductService.Infrastructure.Abstractions.EventHandlers.Abstractions;

namespace ProductService.Infrastructure.Saga.Dispatchers;

public class OrderEventDispatcher(IOrderCreatedEventHandler eventHandler, ILogger<OrderEventDispatcher> logger) : ICapSubscribe
{
    [CapSubscribe(Topics.Orders.OrdersTopic)]
    public async Task HandleOrderEventAsync(IntegrationEvent @event)
    {
        logger.LogInformation("Received event: {EventType} with CorrelationId: {CorrelationId}", @event.GetType().Name, @event.CorrelationId);
        switch (@event)
        {
            case OrderCreatedEvent createdEvent:
                await eventHandler.HandleAsync(createdEvent);
                break;
            
            case OrderCancelledEvent:
                await Task.Delay(200);
                break;
        }
    }
}
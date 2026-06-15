using DotNetCore.CAP;
using IntegrationEvents;
using IntegrationEvents.IntegrationEvents;
using ProductService.Infrastructure.Abstractions.EventHandlers.Abstractions;

namespace ProductService.Infrastructure.Dispatchers;

public class OrderEventDispatcher(IOrderCreatedEventHandler eventHandler) : ICapSubscribe
{
    [CapSubscribe(Topics.Orders.OrdersTopic)]
    public async Task HandleOrderEventAsync(IntegrationEvent @event)
    {
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
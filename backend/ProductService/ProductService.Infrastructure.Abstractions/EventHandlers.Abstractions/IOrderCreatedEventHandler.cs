using IntegrationEvents.IntegrationEvents;
using IntegrationEvents.IntegrationEvents.Order;

namespace ProductService.Infrastructure.Abstractions.EventHandlers.Abstractions;

public interface IOrderCreatedEventHandler
{
    Task HandleAsync(OrderCreatedEvent @event);
}
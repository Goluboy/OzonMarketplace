using IntegrationEvents.IntegrationEvents;

namespace ProductService.Infrastructure.Abstractions.EventHandlers.Abstractions;

public interface IOrderCreatedEventHandler
{
    Task HandleAsync(OrderCreatedEvent @event);
}
using MassTransit;
using OrderService.IntegrationEvents.IntegrationEvents;

namespace OrderService.Infrastructure.EventBus.Consumers;

public class PriceCalculatedConsumer : IConsumer<PriceCalculatedEvent>
{
    public Task Consume(ConsumeContext<PriceCalculatedEvent> context)
    {
        throw new NotImplementedException();
    }
}
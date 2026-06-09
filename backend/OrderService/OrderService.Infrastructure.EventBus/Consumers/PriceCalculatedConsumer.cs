using IntegrationEvents.IntegrationEvents;
using MassTransit;

namespace OrderService.Infrastructure.EventBus.Consumers;

public class PriceCalculatedConsumer : IConsumer<PriceCalculatedEvent>
{
    public Task Consume(ConsumeContext<PriceCalculatedEvent> context)
    {
        throw new NotImplementedException();
    }
}
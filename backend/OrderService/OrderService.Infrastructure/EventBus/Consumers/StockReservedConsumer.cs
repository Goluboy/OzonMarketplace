using MassTransit;
using OrderService.IntegrationEvents.IntegrationEvents;

namespace OrderService.Infrastructure.EventBus.Consumers;

public class StockReservedConsumer : IConsumer<StockReservedEvent>
{
    public Task Consume(ConsumeContext<StockReservedEvent> context)
    {
        throw new NotImplementedException();
    }
}
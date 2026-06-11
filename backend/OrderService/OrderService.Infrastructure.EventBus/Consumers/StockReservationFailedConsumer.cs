using IntegrationEvents.IntegrationEvents;
using MassTransit;

namespace OrderService.Infrastructure.EventBus.Consumers;

public class StockReservationFailedConsumer : IConsumer<StockReservationFailedEvent>
{
    public Task Consume(ConsumeContext<StockReservationFailedEvent> context)
    {
        throw new NotImplementedException();
    }
}
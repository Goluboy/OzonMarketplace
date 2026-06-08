using IntegrationEvents.Shared;
using MassTransit;

namespace OrderService.Infrastructure.EventBus.Consumers;

public class OrderSagaTimeoutConsumer : IConsumer<OrderSagaTimeout>
{
    public Task Consume(ConsumeContext<OrderSagaTimeout> context)
    {
        throw new NotImplementedException();
    }
}
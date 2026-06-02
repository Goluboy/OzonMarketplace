using MassTransit;
using OrderService.IntegrationEvents.Shared;

namespace OrderService.Infrastructure.EventBus.Consumers;

public class OrderSagaTimeoutConsumer : IConsumer<OrderSagaTimeout>
{
    public Task Consume(ConsumeContext<OrderSagaTimeout> context)
    {
        throw new NotImplementedException();
    }
}
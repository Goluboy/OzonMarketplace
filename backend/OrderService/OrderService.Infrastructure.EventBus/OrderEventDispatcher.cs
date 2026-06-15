using DotNetCore.CAP;
using IntegrationEvents;
using IntegrationEvents.IntegrationEvents;
using IntegrationEvents.IntegrationEvents.Order;
using OrderService.Infrastructure.EventBus.Consumers;

namespace OrderService.Infrastructure.EventBus;

public class OrderEventDispatcher(
    OrderCreatedConsumer orderCreatedConsumer,
    OrderSagaTimeoutConsumer orderSagaTimeoutConsumer,
    OrderCancelledConsumer orderCancelledConsumer)
    : ICapSubscribe
{
    [CapSubscribe(Topics.Orders.OrdersTopic)]
    public async Task HandleAsync(
        IntegrationEvent @event,
        [FromCap] CapHeader header,
        CancellationToken cancellationToken)
    {
        switch (@event)
        {
            case OrderCreatedEvent orderCreated:
                await orderCreatedConsumer.HandleAsync(orderCreated, header, cancellationToken);
                break;

            case OrderTimeoutEvent sagaTimeout:
                await orderSagaTimeoutConsumer.HandleAsync(sagaTimeout, header, cancellationToken);
                break;

            case OrderCancelledEvent orderCancelled:
                await orderCancelledConsumer.HandleAsync(orderCancelled, header, cancellationToken);
                break;
        }
    }
}
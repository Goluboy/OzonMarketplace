using DotNetCore.CAP;
using IntegrationEvents;
using IntegrationEvents.IntegrationEvents;
using IntegrationEvents.IntegrationEvents.Order;
using Microsoft.Extensions.Logging;
using OrderService.Infrastructure.EventBus.Consumers;

namespace OrderService.Infrastructure.EventBus.Dispatcher;

public class OrderEventDispatcher(
    OrderCreatedConsumer orderCreatedConsumer,
    OrderSagaTimeoutConsumer orderSagaTimeoutConsumer,
    OrderCancelledConsumer orderCancelledConsumer,
    ILogger<OrderEventDispatcher> logger)
    : ICapSubscribe
{
    [CapSubscribe(Topics.Orders.OrdersTopic)]
    public async Task HandleAsync(
        IntegrationEvent @event,
        [FromCap] CapHeader header,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Received event: {EventType} with CorrelationId: {CorrelationId}", @event.GetType().Name, @event.CorrelationId);
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
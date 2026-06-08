using IntegrationEvents.IntegrationEvents;
using IntegrationEvents.Shared;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace OrderService.Infrastructure.EventBus.EventBus.Consumers;

public class OrderSagaTimeoutConsumer : IConsumer<OrderSagaTimeout>
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<OrderSagaTimeoutConsumer> _logger;

    public OrderSagaTimeoutConsumer(IPublishEndpoint publishEndpoint, ILogger<OrderSagaTimeoutConsumer> logger)
    {
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderSagaTimeout> context)
    {
        var correlationId = context.Message.CorrelationId;
        _logger.LogInformation("Order saga timeout received for {CorrelationId}", correlationId);
        
        var cancelEvent = new OrderCancelledEvent
        {
            CorrelationId = correlationId,
            Reason = "TIMEOUT",
            ItemsToRelease = new List<Guid>()
        };

        await _publishEndpoint.Publish(cancelEvent, context.CancellationToken);

        _logger.LogInformation("Published OrderCancelledEvent for {CorrelationId} due to timeout", correlationId);
    }
}

using DotNetCore.CAP;
using DotNetCore.CAP.Messages;
using IntegrationEvents;
using IntegrationEvents.IntegrationEvents;
using IntegrationEvents.Shared;
using Microsoft.Extensions.Logging;
using OrderService.Domain.Interfaces.Persistence;

namespace OrderService.Infrastructure.EventBus.Consumers;

public class OrderCreatedConsumer(
    IProcessedEventsRepository processedEvents,
    IUnitOfWork unitOfWork,
    ICapPublisher capPublisher)
    : BaseConsumer(processedEvents, unitOfWork), ICapSubscribe
{
    [CapSubscribe(Topics.Orders.Created)]
    public async Task HandleAsync(
        OrderCreatedEvent orderCreatedEvent,
        [FromCap] CapHeader header,
        CancellationToken cancellationToken)
    {

        await ExecuteWithIdempotencyAsync(
            header,
            nameof(OrderCreatedEvent),
            async () =>
            {
                await capPublisher.PublishDelayAsync(
                    TimeSpan.FromMinutes(15),
                    Topics.Orders.SagaTimeout,
                    new OrderSagaTimeout() { CorrelationId = orderCreatedEvent.OrderId },
                    cancellationToken: cancellationToken);
            },
            cancellationToken);
    }
}
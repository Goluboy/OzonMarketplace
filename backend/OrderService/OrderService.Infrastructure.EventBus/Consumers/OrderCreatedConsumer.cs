using DotNetCore.CAP;
using IntegrationEvents.IntegrationEvents;
using IntegrationEvents.Shared;
using Microsoft.Extensions.Logging;
using OrderService.Domain.Interfaces.Persistence;

namespace OrderService.Infrastructure.EventBus.Consumers;

public class OrderCreatedConsumer(
    IProcessedEventsRepository processedEvents,
    IUnitOfWork unitOfWork,
    ICapPublisher capPublisher) : BaseConsumer(processedEvents, unitOfWork), ICapSubscribe
{
    [CapSubscribe("orders.created")]
    public async Task HandleAsync(
        OrderCreatedEvent @event,
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
                    "order-service-timeout",
                    new OrderSagaTimeout
                    {
                        CorrelationId = @event.CorrelationId
                    },
                    cancellationToken: cancellationToken);


                foreach (var item in @event.Items)
                {
                    await capPublisher.PublishAsync(
                        "products.stock.reserve",
                        new StockReservedEvent
                        {
                            CorrelationId = @event.CorrelationId,
                            ReservedItems = new List<ReservedItemDto>
                            {
                                new ReservedItemDto(item.ProductId, item.Quantity)
                            }
                        },
                        cancellationToken: cancellationToken);
                }

                await capPublisher.PublishAsync(
                    "prices.calculate",
                    new PriceCalculatedEvent
                    {
                        CorrelationId = @event.CorrelationId
                    },
                    cancellationToken: cancellationToken);
            },
            cancellationToken);
    }
}
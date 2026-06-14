using DotNetCore.CAP;
using IntegrationEvents;
using IntegrationEvents.Shared;
using OrderService.Domain.Interfaces.Persistence;
using OrderService.UseCases.Commands.Commands;
using OrderService.UseCases.Commands.Interfaces;

namespace OrderService.Infrastructure.EventBus.Consumers;

public class OrderSagaTimeoutConsumer(
    IOrderRepository orderRepository,
    IProcessedEventsRepository processedEvents,
    IUnitOfWork unitOfWork,
    ICapPublisher capPublisher,
    ICommandHandler<ForceCancelOrderCommand, bool> cancelOrderHandler)
    : BaseConsumer(processedEvents, unitOfWork), ICapSubscribe
{
    [CapSubscribe(Topics.Orders.SagaTimeout)]
    public async Task HandleAsync(
       OrderSagaTimeout orderSagaTimeout,
       [FromCap] CapHeader header,
       CancellationToken cancellationToken)
    {
        await ExecuteWithIdempotencyAsync(
            header,
            nameof(OrderSagaTimeout),
            async () =>
            {
                var command = new ForceCancelOrderCommand(orderSagaTimeout.CorrelationId, "TimeOut");
                await cancelOrderHandler.HandleAsync(command, cancellationToken);
            },
            cancellationToken);
    }
}
using DotNetCore.CAP;
using IntegrationEvents.IntegrationEvents;
using IntegrationEvents.IntegrationEvents.Order;
using IntegrationEvents.Shared;
using OrderService.Domain.Interfaces.Persistence;
using OrderService.UseCases.Commands.Commands;
using OrderService.UseCases.Commands.Interfaces;

namespace OrderService.Infrastructure.EventBus.Consumers;

public class OrderSagaTimeoutConsumer(ICommandHandler<ForceCancelOrderCommand, bool> cancelOrderHandler)
    : BaseConsumer
{
    public async Task HandleAsync(
       OrderTimeoutEvent orderSagaTimeout,
       CapHeader header,
       CancellationToken cancellationToken)
    {
        await ExecuteWithIdempotencyAsync(
            header,
            nameof(OrderTimeoutEvent),
            async () =>
            {
                var command = new ForceCancelOrderCommand(orderSagaTimeout.CorrelationId, "TimeOut");
                await cancelOrderHandler.HandleAsync(command, cancellationToken);
            },
            cancellationToken);
    }
}
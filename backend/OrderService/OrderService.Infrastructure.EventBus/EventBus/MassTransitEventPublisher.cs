using MassTransit;
using OrderService.Application.Commands.Common;

namespace OrderService.Infrastructure.EventBus.EventBus;

public class MassTransitEventPublisher(ISendEndpointProvider provider) : IEventPublisher
{
    public async Task PublishAsync<T>(T message) where T : class
    {
        var endpoint = await provider.GetSendEndpoint(new Uri("kafka:orders"));
        await endpoint.Send(message);
    }
}
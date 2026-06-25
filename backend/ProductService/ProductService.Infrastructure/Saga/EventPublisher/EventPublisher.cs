using DotNetCore.CAP;
using ProductService.Infrastructure.Abstractions.EventPublisher.Abstractions;

namespace ProductService.Infrastructure.Saga.EventPublisher;

public class EventPublisher(ICapPublisher capPublisher) : IEventPublisher
{
    public Task PublishAsync<T>(string topicName, T @event, IDictionary<string, string?> headers, CancellationToken ct = default) where T : class
    {
        return capPublisher.PublishAsync(topicName, @event, headers, ct);
    }
}
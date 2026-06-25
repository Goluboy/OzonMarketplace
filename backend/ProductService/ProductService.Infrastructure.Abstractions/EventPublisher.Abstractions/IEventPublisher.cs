namespace ProductService.Infrastructure.Abstractions.EventPublisher.Abstractions;

public interface IEventPublisher
{
    Task PublishAsync<T>(
        string topicName, 
        T @event, 
        IDictionary<string, string?> headers, 
        CancellationToken ct = default) where T : class;
}
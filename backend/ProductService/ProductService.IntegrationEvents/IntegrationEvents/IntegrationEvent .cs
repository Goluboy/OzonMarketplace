namespace ProductService.IntegrationEvents.IntegrationEvents;

public abstract record IntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
    public Guid CorrelationId { get; init; } // = OrderId для связки шагов SAGA
}

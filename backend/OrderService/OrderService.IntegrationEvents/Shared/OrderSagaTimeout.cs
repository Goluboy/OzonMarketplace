namespace OrderService.IntegrationEvents.Shared;

public record OrderSagaTimeout
{
    public Guid CorrelationId { get; init; } // = OrderId
}
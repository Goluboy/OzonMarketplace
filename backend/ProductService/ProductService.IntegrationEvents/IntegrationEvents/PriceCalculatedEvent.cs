namespace ProductService.IntegrationEvents.IntegrationEvents;

public record PriceCalculatedEvent : IntegrationEvent
{
    public decimal TotalAmount { get; init; }
    public string Currency { get; init; } = "RUB";
}
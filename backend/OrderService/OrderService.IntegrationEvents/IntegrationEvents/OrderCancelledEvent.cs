namespace OrderService.IntegrationEvents.IntegrationEvents;

public record OrderCancelledEvent : IntegrationEvent
{
    public string Reason { get; init; } = string.Empty; // "Timeout", "StockFailed", "UserCancelled"
    public List<Guid> ItemsToRelease { get; init; } = new();
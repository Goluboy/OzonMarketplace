using OrderService.IntegrationEvents.Shared;

namespace OrderService.IntegrationEvents.IntegrationEvents;

public record OrderCreatedEvent : IntegrationEvent
{
    public List<OrderItemDto> Items { get; init; } = new();
    public string CustomerEmail { get; init; } = string.Empty;
    public string DeliveryAddress { get; init; } = string.Empty;
}
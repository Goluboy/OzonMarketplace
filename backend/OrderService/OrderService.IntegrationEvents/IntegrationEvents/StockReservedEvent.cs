using OrderService.IntegrationEvents.Shared;

namespace OrderService.IntegrationEvents.IntegrationEvents;

public record StockReservedEvent : IntegrationEvent
{
    public List<ReservedItemDto> ReservedItems { get; init; } = new();
}
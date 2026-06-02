using ProductService.IntegrationEvents.Shared;

namespace ProductService.IntegrationEvents.IntegrationEvents;

public record StockReservedEvent : IntegrationEvent
{
    public List<ReservedItemDto> ReservedItems { get; init; } = new();
}
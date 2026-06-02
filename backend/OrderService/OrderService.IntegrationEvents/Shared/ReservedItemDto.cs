namespace OrderService.IntegrationEvents.Shared;

public record ReservedItemDto(Guid ProductId, int Quantity);
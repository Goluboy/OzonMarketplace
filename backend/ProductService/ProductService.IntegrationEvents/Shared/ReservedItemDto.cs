namespace ProductService.IntegrationEvents.Shared;

public record ReservedItemDto(Guid ProductId, int Quantity);
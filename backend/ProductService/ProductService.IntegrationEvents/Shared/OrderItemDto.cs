namespace ProductService.IntegrationEvents.Shared;

public record OrderItemDto(Guid ProductId, int Quantity);
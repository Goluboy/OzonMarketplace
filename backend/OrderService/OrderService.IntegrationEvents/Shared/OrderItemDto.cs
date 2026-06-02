namespace OrderService.IntegrationEvents.Shared;

public record OrderItemDto(Guid ProductId, int Quantity);
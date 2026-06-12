namespace OrderService.Http.Dtos;

public record OrderItemCreate(Guid ProductId, int Quantity);
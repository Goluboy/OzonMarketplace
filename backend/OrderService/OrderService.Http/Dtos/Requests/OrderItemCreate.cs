namespace OrderService.Http.Dtos.Requests;

public record OrderItemCreate(Guid ProductId, int Quantity);
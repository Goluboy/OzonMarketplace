namespace OrderService.Http.Dtos;

public record CreateOrderRequest(
    string CustomerName,
    string CustomerEmail,
    List<OrderItemCreate> Items,
    string? DeliveryAddress = null);

public record OrderItemCreate(Guid ProductId, int Quantity);

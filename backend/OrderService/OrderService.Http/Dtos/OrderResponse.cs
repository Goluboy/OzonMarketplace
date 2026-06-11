namespace OrderService.UseCases.Queries.Dtos;

public record OrderResponse(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    string CustomerEmail,
    string? DeliveryAddress,
    string Status,
    decimal TotalAmount,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    List<OrderItemResponse> Items);
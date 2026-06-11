namespace OrderService.UseCases.Queries.Models;

public record OrderModel(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    string CustomerEmail,
    string? DeliveryAddress,
    string Status,
    decimal TotalAmount,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    List<OrderItemModel> Items);
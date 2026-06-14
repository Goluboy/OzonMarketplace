using OrderService.Domain.ValueObjects;

namespace OrderService.UseCases.Queries.Models;

public record OrderModel(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    string CustomerEmail,
    string? DeliveryAddress,
    OrderStatus Status,
    decimal TotalAmount,
    string Currency,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    List<OrderItemModel> Items);
using OrderService.Domain.ValueObjects;

namespace OrderService.UseCases.Queries.Models;

public record OrderModel(
    Guid Id,
    OrderStatus Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    string CustomerName,
    string CustomerEmail,
    string? DeliveryAddress,
    Money TotalAmount,
    List<OrderItemModel> Items,
    Guid CustomerId,
    List<AuditLogItemModel>? AuditLog);
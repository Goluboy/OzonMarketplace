using OrderService.Domain.ValueObjects;

namespace OrderService.Http.Dtos;

public record OrderDto(
    Guid Id,
    OrderStatus Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    string CustomerName,
    string CustomerEmail,
    string? DeliveryAddress,
    MoneyDto TotalAmount,
    List<OrderItemDto> Items);

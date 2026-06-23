using OrderService.Domain.ValueObjects;
using OrderService.Http.Dtos.Shared;

namespace OrderService.Http.Dtos.Requests;

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
using OrderService.Domain.ValueObjects;
using OrderService.Http.Dtos.Shared;

namespace OrderService.Http.Dtos.Requests;

public record AdminOrderDto(
    Guid Id,
    OrderStatus Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    string CustomerName,
    string CustomerEmail,
    string? DeliveryAddress,
    MoneyDto TotalAmount,
    List<OrderItemDto> Items,
    Guid CustomerId)
    : OrderDto(Id, Status, CreatedAt, UpdatedAt, CustomerName, CustomerEmail, DeliveryAddress, TotalAmount, Items);
using OrderService.Http.Dtos.Shared;

namespace OrderService.Http.Dtos.Requests;

public record CreateOrderRequest(
    string? CustomerName,
    string? CustomerEmail,
    List<OrderItemCreate> Items,
    string? DeliveryAddress);
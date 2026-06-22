using OrderService.Http.Dtos.Shared;

namespace OrderService.Http.Dtos.Requests;

public record OrderItemDto(
    Guid ProductId,
    string ProductName,
    int Quantity,
    MoneyDto PriceAtPurchase);
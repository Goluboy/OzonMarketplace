namespace OrderService.Http.Dtos;

public record OrderItemDto(
    Guid ProductId,
    string ProductName,
    int Quantity,
    MoneyDto PriceAtPurchase);

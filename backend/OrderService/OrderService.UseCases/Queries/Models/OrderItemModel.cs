using OrderService.Domain.ValueObjects;

namespace OrderService.UseCases.Queries.Models;

public record OrderItemModel(
    Guid ProductId,
    string ProductName,
    int Quantity,
    Money PriceAtPurchase);
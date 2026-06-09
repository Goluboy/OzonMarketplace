namespace OrderService.UseCases.Queries.Models;

public record OrderItemModel(
    Guid Id,
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal Price,
    decimal Subtotal);
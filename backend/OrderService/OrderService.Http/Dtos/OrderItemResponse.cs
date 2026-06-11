namespace OrderService.UseCases.Queries.Dtos;

public record OrderItemResponse(
    Guid Id,
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal Price,
    decimal Subtotal);
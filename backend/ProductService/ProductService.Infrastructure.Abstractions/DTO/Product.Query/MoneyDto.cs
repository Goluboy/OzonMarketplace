namespace ProductService.Infrastructure.Abstractions.DTO.Product.Query;

public record MoneyDto(
    decimal Amount,
    string Currency);
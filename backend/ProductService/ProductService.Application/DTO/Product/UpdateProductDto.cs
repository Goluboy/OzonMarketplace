using ProductService.Domain.ValueObjects;
using ProductService.Infrastructure.Abstractions.DTO.Product.Query;

namespace ProductService.Application.DTO.Product;

public record UpdateProductDto(
    Guid ProductId,
    string Name,
    string Description,
    MoneyDto Price,
    int CategoryId,
    List<string> ImagesUrl);
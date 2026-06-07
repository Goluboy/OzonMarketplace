using ProductService.Domain.ValueObjects;
using ProductService.Infrastructure.Abstractions.DTO.Product.Query;

namespace ProductService.Application.DTO.Product;

public record CreateProductDto(
    long Sku,
    string Name,
    string Description,
    MoneyDto Price,
    int CategoryId,
    List<string> ImagesUrl);
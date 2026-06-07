using ProductService.Infrastructure.Abstractions.DTO.Product.Query;

namespace ProductService.Application.DTO.Product;

public record ProductCardsPage(
    IReadOnlyCollection<ProductCardDto> Items,
    string? NextCursor,
    int PageSize);
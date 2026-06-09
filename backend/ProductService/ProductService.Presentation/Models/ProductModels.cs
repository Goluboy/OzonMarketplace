namespace ProductService.Presentation.Models;

public record MoneyDto(
    string Amount,
    string Currency
);

public record ProductImageDto(string Url);

public record UpsertProductRequest(
    long Sku,
    string Name,
    string Description,
    MoneyDto Price,
    int CategoryId,
    List<ProductImageDto> Images
);

public record ProductResponse(
    Guid Id,
    long Sku,
    Guid SellerId,
    string Name,
    string Description,
    MoneyDto Price,
    int CategoryId,
    string CategoryName,
    string CategoryPath,
    List<ProductImageDto> Images,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

public record ProductCardResponse(
    Guid Id,
    string Name,
    MoneyDto Price,
    string ImageUrl,
    int CategoryId
);

public record ProductCursorPagedResponse(
    IReadOnlyCollection<ProductCardResponse> Items,
    string? NextCursor,
    int PageSize
);
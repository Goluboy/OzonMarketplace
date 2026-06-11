namespace ProductService.Presentation.Models;

public record MoneyHttpDto(
    string Amount,
    string Currency);

public record ProductImageHttpDto(string Url);

public record CreateProductRequest(
    long Sku,
    string Name,
    string Description,
    MoneyHttpDto Price,
    int CategoryId,
    List<ProductImageHttpDto> Images);

public record UpdateProductRequest(
    string Name,
    string Description,
    MoneyHttpDto Price,
    int CategoryId,
    List<ProductImageHttpDto> Images);

public record ProductSearchFilterRequest(
    string? Search,
    int? CategoryId,
    MoneyHttpDto? MinPrice, 
    MoneyHttpDto? MaxPrice,
    string SortBy,    // "name", "price", "createdAt"
    string SortOrder, // "asc", "desc"
    string? Cursor,
    int PageSize);

public record ProductResponse(
    Guid Id,
    long Sku,
    Guid SellerId,
    string Name,
    string Description,
    MoneyHttpDto Price,
    int CategoryId,
    string CategoryName,
    string CategoryPath,
    List<ProductImageHttpDto> Images,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record ProductCardResponse(
    Guid Id,
    string Name,
    MoneyHttpDto Price,
    string ImageUrl,
    int CategoryId);

public record ProductCursorPagedResponse(
    IReadOnlyCollection<ProductCardResponse> Items,
    string? NextCursor,
    int PageSize);
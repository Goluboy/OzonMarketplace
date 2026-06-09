namespace ProductService.Infrastructure.Abstractions.DTO.Product.Query;

public record ProductSearchFilter(
    string? Search,
    int? CategoryId,
    MoneyDto? MinPrice, 
    MoneyDto? MaxPrice,
    string SortBy,    // "name", "price", "createdAt"
    string SortOrder, // "asc", "desc"
    string? Cursor,
    int PageSize = 10);
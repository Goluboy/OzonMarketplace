namespace ProductService.Application.DTO.Category;

public record CategoriesResponse(
    IReadOnlyCollection<CategoryDto> Categories,
    string Etag,
    bool IsModified);
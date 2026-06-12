namespace ProductService.Application.DTO.Category;

public record CategoriesResponse(
    IReadOnlyCollection<CategoryDto> Categories,
    string ETag,
    bool IsModified);
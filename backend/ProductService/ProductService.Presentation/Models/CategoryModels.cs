namespace ProductService.Presentation.Models;

public record UpsertCategoryRequest(
    string Name,
    string Path
);

public record CategoryResponse(
    int Id,
    string Name,
    string Path
);
namespace ProductService.Presentation.Models;

public record SaveCategoryRequest(
    string Name,
    string Path
);

public record CategoryResponse(
    int Id,
    string Name,
    string Path
);
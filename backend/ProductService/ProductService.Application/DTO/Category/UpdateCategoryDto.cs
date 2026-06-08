namespace ProductService.Application.DTO.Category;

public record UpdateCategoryDto(
    int Id,
    string Name,
    string Path);
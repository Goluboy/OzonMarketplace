namespace ProductService.Infrastructure.Abstractions.DTO.Category;

public record CategoryDto(
    int Id,
    string Name,
    string Path);
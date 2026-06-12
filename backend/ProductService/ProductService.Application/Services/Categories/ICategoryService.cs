
using ProductService.Application.DTO.Category;

namespace ProductService.Application.Services.Categories;

public interface ICategoryService
{
    Task<CategoriesResponse> GetAllAsync(string? eTag, CancellationToken ct);
    Task<CategoryDto> CreateAsync(CreateCategoryDto dto, CancellationToken ct);
    Task<CategoryDto> UpdateAsync(UpdateCategoryDto dto, CancellationToken ct);
    Task DeleteAsync(int id, CancellationToken ct);
}
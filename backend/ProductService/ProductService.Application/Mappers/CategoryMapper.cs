using ProductService.Application.DTO.Category;
using ProductService.Domain.Entities;

namespace ProductService.Application.Mappers;

public static class CategoryMapper
{
    public static CategoryDto ToDto(this Category category)
    {
        return new CategoryDto(
            category.Id,
            category.Name,
            category.Path);
    }
}
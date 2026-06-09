using ProductService.Application.DTO.Category;
using ProductService.Presentation.Models;

namespace ProductService.Presentation.Mappers;

public static class CategoryMapper
{
    public static CategoryResponse ToHttpResponse(this CategoryDto dto)
    {
        return new CategoryResponse(
            dto.Id,
            dto.Name,
            dto.Path);
    }

    extension(UpsertCategoryRequest request)
    {
        public CreateCategoryDto ToCreateDto()
        {
            return new CreateCategoryDto(request.Name, request.Path);
        }

        public UpdateCategoryDto ToUpdateDto(int categoryId)
        {
            return new UpdateCategoryDto(categoryId, request.Name, request.Path);
        }
    }
}
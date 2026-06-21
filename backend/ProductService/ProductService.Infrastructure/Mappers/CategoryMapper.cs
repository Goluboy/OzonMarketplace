using ProductService.Domain.Entities;
using ProductService.Infrastructure.DAO;

namespace ProductService.Infrastructure.Mappers;

public static class CategoryMapper
{
    public static Category ToDomain(this CategoryDao dao)
    {
        return Category.Reconstruct(dao.Id, dao.Name, dao.Path);
    }

    public static CategoryDao ToDao(this Category entity)
    {
        return new CategoryDao
        (
            Id: entity.Id,
            Name: entity.Name,
            Path: entity.Path
        );
    }
}
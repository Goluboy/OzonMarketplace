using ProductService.Domain.Entities;

namespace ProductService.Infrastructure.Abstractions.Repository.Abstractions;

public interface ICategoryRepository
{
    Task<IReadOnlyCollection<Category>> GetAllAsync();
    Task<Category?> GetByIdAsync(int id);
    Task<int> AddAsync(Category category);
    Task<bool> UpdateAsync(Category category);
    Task DeleteAsync(int id);
}
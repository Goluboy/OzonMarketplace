using ProductService.Domain.Entities;

namespace ProductService.Infrastructure.Abstractions.Repository.Abstractions;

public interface ICategoryRepository
{
    Task<IReadOnlyCollection<Category>> GetAllAsync(CancellationToken ct = default);
    Task<Category?> GetAsync(int id);
    Task<int> AddAsync(Category category);
    Task<bool> UpdateAsync(Category category);
    Task DeleteAsync(int id);
}
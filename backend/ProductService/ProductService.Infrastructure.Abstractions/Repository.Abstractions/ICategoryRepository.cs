using ProductService.Domain.Entities;

namespace ProductService.Infrastructure.Abstractions.Repository.Abstractions;

public interface ICategoryRepository
{
    Task<IReadOnlyCollection<Category>> GetAllAsync(CancellationToken ct = default);
    Task<Category?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<int> AddAsync(Category category, CancellationToken ct = default);
    Task<bool> UpdateAsync(Category category, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
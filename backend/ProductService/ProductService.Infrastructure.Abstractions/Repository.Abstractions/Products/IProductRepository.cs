using ProductService.Domain.Entities;
using ProductService.Infrastructure.Abstractions.DTO.Product.Query;

namespace ProductService.Infrastructure.Abstractions.Repository.Abstractions.Products;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id);
    Task AddAsync(Product product);
    Task UpdateAsync(Product product);
    Task DeleteAsync(Guid id);
}
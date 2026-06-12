using ProductService.Domain.Entities;
using ProductService.Infrastructure.Abstractions.Repository.Abstractions.Products;
using ProductService.Infrastructure.Mappers;
using Redis.Service;

namespace ProductService.Infrastructure.Repository.Decorators;

public class CachedProductRepository(IProductRepository inner,  ICacheService cache) : IProductRepository
{
    private static readonly TimeSpan CacheExpiry = TimeSpan.FromHours(12);
    private static string GetProductKey(Guid id) => $"products:{id:N}";
    
    public async Task<Product?> GetAsync(Guid id)
    {
        var key = GetProductKey(id);
        
        var cachedProduct = await cache.GetAsync<Product>(key);
        if (cachedProduct != null)
        {
            return cachedProduct;
        }
        
        var product = await inner.GetAsync(id);

        if (product != null)
        {
            await cache.SetAsync(key, product.ToDao(), CacheExpiry);
        }
        
        return product;
    }

    public Task AddAsync(Product product) => inner.AddAsync(product);

    public Task UpdateAsync(Product product) => inner.UpdateAsync(product);

    public Task DeleteAsync(Guid id) => inner.DeleteAsync(id);
}
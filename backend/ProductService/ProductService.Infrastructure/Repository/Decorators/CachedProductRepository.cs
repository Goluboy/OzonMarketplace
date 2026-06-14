using ProductService.Domain.Entities;
using ProductService.Domain.ValueObjects;
using ProductService.Infrastructure.Abstractions.Repository.Abstractions.Products;
using ProductService.Infrastructure.DAO;
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
        
        var cachedProduct = await cache.GetAsync<ProductDao>(key);
        if (cachedProduct != null)
        {
            return Product.Reconstruct(
                id: cachedProduct.Id,
                sellerId: cachedProduct.SellerId,
                sku: cachedProduct.Sku,
                name: cachedProduct.Name,
                description: cachedProduct.Description,
                price: new Money(cachedProduct.PriceAmount, cachedProduct.PriceCurrency),
                categoryId: cachedProduct.CategoryId,
                createdAt: cachedProduct.CreatedAt,
                updatedAt: cachedProduct.UpdatedAt,
                version: cachedProduct.Version,
                images: cachedProduct.Images.Select(url => new ProductImage(url.Url)).ToList());
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
using Microsoft.Extensions.Logging;
using ProductService.Domain.Entities;
using ProductService.Domain.ValueObjects;
using ProductService.Infrastructure.Abstractions.Repository.Abstractions.Products;
using ProductService.Infrastructure.Abstractions.UnitOfWork.Abstractions;
using ProductService.Infrastructure.DAO;
using ProductService.Infrastructure.Mappers;
using Redis.Service;

namespace ProductService.Infrastructure.Repository.Decorators;

public class CachedProductRepository(IProductRepository inner,  ICacheService cache, IUnitOfWork unitOfWork,
    ILogger<CachedProductRepository> logger) : IProductRepository
{
    private static string GetDetailsKey(Guid id) => $"products:details:{id:N}";
    private static string GetCardKey(Guid id) => $"products:card:{id:N}";
    
    public Task<Product?> GetAsync(Guid id) => inner.GetAsync(id);

    public Task AddAsync(Product product) => inner.AddAsync(product);

    public async Task UpdateAsync(Product product)
    {
        await inner.UpdateAsync(product);
        unitOfWork.RegisterPostCommitAction(() => InvalidateReadCacheAsync(product.Id));
    }

    public async Task DeleteAsync(Guid id)
    {
        await inner.DeleteAsync(id);
        unitOfWork.RegisterPostCommitAction(() => InvalidateReadCacheAsync(id));
    }
    
    private async Task InvalidateReadCacheAsync(Guid productId)
    {
        try
        {
            var keysToRemove = new[]
            {
                GetDetailsKey(productId),
                GetCardKey(productId) 
            };

            await cache.RemoveManyAsync(keysToRemove);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AAn error occured while trying to invalidate cache for Product {productId}", productId);
        }
    }
}
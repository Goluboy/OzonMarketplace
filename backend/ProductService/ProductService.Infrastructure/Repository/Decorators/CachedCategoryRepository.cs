using Microsoft.Extensions.Logging;
using ProductService.Domain.Entities;
using ProductService.Infrastructure.Abstractions.Repository.Abstractions;
using ProductService.Infrastructure.Abstractions.UnitOfWork.Abstractions;
using ProductService.Infrastructure.DAO;
using ProductService.Infrastructure.Mappers;
using Redis.Service;

namespace ProductService.Infrastructure.Repository.Decorators;

public class CachedCategoryRepository(
    ICategoryRepository inner,
    ICacheService cache,
    IUnitOfWork unitOfWork,
    ILogger<CachedCategoryRepository> logger)
    : ICategoryRepository
{
    private const string AllCategoriesKey = "categories:all";
    private static readonly TimeSpan CacheExpiry = TimeSpan.FromHours(24);

    private static string GetCategoryKey(int id) => $"categories:item:{id}";
    
    public async Task<IReadOnlyCollection<Category>> GetAllAsync(CancellationToken ct = default)
    {
        var cachingCategories = await cache.GetAsync<IReadOnlyCollection<CategoryDao>>(AllCategoriesKey, ct);
        if (cachingCategories != null)
        {
            return cachingCategories
                .Select(m => Category.Reconstruct(m.Id, m.Name, m.Path))
                .ToList();
        }
        
        var categories = await inner.GetAllAsync(ct);
        
        var categoriesDaos = categories.Select(category => category.ToDao());
        
        await cache.SetAsync(AllCategoriesKey, categoriesDaos, CacheExpiry, ct);
        
        return categories;
    }

    public async Task<Category?> GetAsync(int id)
    {
        var key = GetCategoryKey(id);
        var cachingCategory = await cache.GetAsync<CategoryDao>(key);
        if (cachingCategory != null)
        {
            return Category.Reconstruct(cachingCategory.Id, cachingCategory.Name, cachingCategory.Path);
        }

        var category = await inner.GetAsync(id);
        if (category != null)
        {
            await cache.SetAsync(key, category.ToDao(), CacheExpiry);
        }
        
        return category;
    }

    public async Task<int> AddAsync(Category category)
    {
        var id = await inner.AddAsync(category);
        
        unitOfWork.RegisterPostCommitAction(() => InvalidateCacheAsync());
        
        return id;
    }
    
    public async Task<bool> UpdateAsync(Category category)
    { 
        var result = await inner.UpdateAsync(category);
        if (result)
        {
            unitOfWork.RegisterPostCommitAction(() => InvalidateCacheAsync(category.Id));
        }
        return result;
    }

    public async Task DeleteAsync(int id)
    {
        await inner.DeleteAsync(id);
        
        unitOfWork.RegisterPostCommitAction(() => InvalidateCacheAsync(id));
    }
    
    private async Task InvalidateCacheAsync(int? id = null)
    {
        try
        {
            var keysToRemove = new List<string> { AllCategoriesKey };
            if (id.HasValue)
            {
                keysToRemove.Add(GetCategoryKey(id.Value));
            }
            
            await cache.RemoveManyAsync(keysToRemove);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occured while trying to invalidate cache for Category {id}.", id);
        }
    }
}
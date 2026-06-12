using ProductService.Domain.Entities;
using ProductService.Infrastructure.Abstractions.Repository.Abstractions;
using ProductService.Infrastructure.Caching;
using Redis.Service;

namespace ProductService.Infrastructure.Repository.Decorators;

public class CachedCategoryRepository(
    ICategoryRepository inner,
    ICacheService cache)
    : ICategoryRepository
{
    private const string AllCategoriesKey = "categories:all";
    private static readonly TimeSpan CacheExpiry = TimeSpan.FromHours(24);

    private static string GetCategoryKey(int id) => $"categories:item:{id}";
    
    public async Task<IReadOnlyCollection<Category>> GetAllAsync(CancellationToken ct = default)
    {
        var cachingCategories = await cache.GetAsync<IReadOnlyCollection<Category>>(AllCategoriesKey, ct);
        if (cachingCategories != null)
        {
            return cachingCategories;
        }
        
        var categories = await inner.GetAllAsync(ct);
        
        await cache.SetAsync(AllCategoriesKey, categories, CacheExpiry, ct);
        
        return categories;
    }

    public async Task<Category?> GetAsync(int id)
    {
        var key = GetCategoryKey(id);
        var cachingCategory = await cache.GetAsync<Category>(key);
        if (cachingCategory != null)
        {
            return cachingCategory;
        }

        var category = await inner.GetAsync(id);
        if (category != null)
        {
            await cache.SetAsync(key, category, CacheExpiry);
        }
        
        return category;
    }

    public Task<int> AddAsync(Category category) => inner.AddAsync(category);
    public Task<bool> UpdateAsync(Category category) => inner.UpdateAsync(category);
    public Task DeleteAsync(int id) => inner.DeleteAsync(id);
}
using ProductService.Domain.Entities;
using ProductService.Infrastructure.Abstractions.Repository.Abstractions;
using ProductService.Infrastructure.Caching;
using ProductService.Infrastructure.DAO;
using ProductService.Infrastructure.Mappers;
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

    public Task<int> AddAsync(Category category) => inner.AddAsync(category);
    public Task<bool> UpdateAsync(Category category) => inner.UpdateAsync(category);
    public Task DeleteAsync(int id) => inner.DeleteAsync(id);
}
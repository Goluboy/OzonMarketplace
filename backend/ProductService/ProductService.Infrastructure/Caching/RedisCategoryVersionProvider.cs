using ProductService.Infrastructure.Abstractions.Caching.Abstractions;
using Redis.Service;

namespace ProductService.Infrastructure.Caching;

internal class RedisCategoryVersionProvider(ICacheService cache) : ICategoryVersionUpdater, ICategoryVersionProvider
{
    private const string CategoryEtagKey = "categories:etag";
    private static readonly TimeSpan CacheExpiry = TimeSpan.FromDays(30);
    
    public async Task UpdateVersionAsync(CancellationToken ct = default)
    {
        var newEtag = GenerateNewEtag();
        await cache.SetAsync(CategoryEtagKey, newEtag, CacheExpiry, ct);
    }

    public async Task<string> GetVersionETagAsync(CancellationToken ct = default)
    {
        var etag = await cache.GetAsync<string>(CategoryEtagKey, ct);
        if (!string.IsNullOrEmpty(etag))
        {
            return etag;
        }
        
        etag = GenerateNewEtag();
        await cache.SetAsync(CategoryEtagKey, etag, CacheExpiry, ct);
        
        return etag;
    }
    
    private static string GenerateNewEtag() => Guid.NewGuid().ToString("N");
}
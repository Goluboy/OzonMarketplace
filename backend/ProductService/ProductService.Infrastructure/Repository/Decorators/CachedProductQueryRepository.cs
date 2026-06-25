using System.Diagnostics.CodeAnalysis;
using ProductService.Infrastructure.Abstractions.DTO.Product.Query;
using ProductService.Infrastructure.Abstractions.Repository.Abstractions.Products;
using Redis.Service;

namespace ProductService.Infrastructure.Repository.Decorators;

[SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract")]
public class CachedProductQueryRepository(IProductQueryRepository inner, ICacheService cache) : IProductQueryRepository
{
    private static readonly TimeSpan DetailsExpiry = TimeSpan.FromHours(24);
    private static readonly TimeSpan CardExpiry = TimeSpan.FromHours(24);
    private static readonly TimeSpan SkuExpiry = TimeSpan.FromHours(12);
    
    private static string GetDetailsKey(Guid id) => $"products:details:{id:N}";
    private static string GetCardKey(Guid id) => $"products:card:{id:N}";
    private static string GetSkuCardsKey(long sku) => $"products:sku-cards:{sku}";
    
    public async Task<IReadOnlyList<ProductCardDto>> GetCardsAsync(long sku)
    {
        var key = GetSkuCardsKey(sku);
        var cached = await cache.GetAsync<IReadOnlyList<ProductCardDto>>(key);
        if (cached != null)
        {
            return cached;
        }

        var cards = await inner.GetCardsAsync(sku);
        if (cards.Count > 0)
        {
            await cache.SetAsync(key, cards, SkuExpiry);
        }

        return cards;
    }

    public async Task<IReadOnlyList<ProductCardDto>> GetCardsAsync(IReadOnlyList<Guid> ids)
    {
        if (ids == null || ids.Count == 0)
        {
            return [];
        }

        var keys = new string[ids.Count];
        for (var i = 0; i < ids.Count; i++)
        {
            keys[i] = GetCardKey(ids[i]);
        }

        var cachedCards = await cache.GetManyAsync<ProductCardDto>(keys);
        
        var results = new List<ProductCardDto>(ids.Count);
        
        List<Guid>? missingIds = null;
        
        for (var i = 0; i < ids.Count; i++)
        {
            var card = cachedCards[i];
            if (card != null)
            {
                results.Add(card);
            }
            else
            {
                missingIds ??= [];
                missingIds.Add(ids[i]);
            }
        }
        
        if (missingIds != null)
        {
            var dbCards = await inner.GetCardsAsync(missingIds);
        
            if (dbCards.Count > 0)
            {
                results.AddRange(dbCards);
                
                var cachePayload = new Dictionary<string, ProductCardDto>(dbCards.Count);
                foreach (var card in dbCards)
                {
                    cachePayload[GetCardKey(card.Id)] = card;
                }

                await cache.SetManyAsync(cachePayload, CardExpiry);
            }
        }
        
        return results;
    }

    public async Task<ProductDetailsDto?> GetDetailsAsync(Guid id)
    {
        var key = GetDetailsKey(id);
        var cachedProduct = await cache.GetAsync<ProductDetailsDto>(key);
        if (cachedProduct != null)
        {
            return cachedProduct;
        }
        
        var details = await inner.GetDetailsAsync(id);
        if (details != null)
        {
            await cache.SetAsync(key, details, DetailsExpiry);
        }
        
        return details;
    }

    public async Task<ProductCardDto?> GetCardAsync(Guid id) => await inner.GetCardAsync(id);
    public Task<ProductPagedIdsDto> GetPagedAsync(ProductSearchFilter filter) => inner.GetPagedAsync(filter);
}
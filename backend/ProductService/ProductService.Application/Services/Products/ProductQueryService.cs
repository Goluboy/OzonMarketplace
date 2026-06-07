using ProductService.Application.DTO.Product;
using ProductService.Application.Exceptions;
using ProductService.Domain.Entities;
using ProductService.Infrastructure.Abstractions.DTO.Product.Query;
using ProductService.Infrastructure.Abstractions.Repository.Abstractions.Products;

namespace ProductService.Application.Services.Products;

public class ProductQueryService(IProductQueryRepository queryRepository) : IProductQueryService
{
    public async Task<ProductCardsPage> GetCatalogAsync(ProductSearchFilter filter, CancellationToken ct = default)
    {
        if (TryGetSku(filter.Search, out var sku))
        {
            var skuCards = await queryRepository.GetCardsAsync(sku, ct);
            
            return new ProductCardsPage(skuCards, NextCursor: null, filter.PageSize);
        }
        
        var dbPagedResult = await queryRepository.GetPagedAsync(filter, ct);
        
        if (!dbPagedResult.ProductIds.Any())
        {
            return new ProductCardsPage([], null, filter.PageSize);
        }
        
        var productIds = dbPagedResult.ProductIds;
        
        var dbCards = await queryRepository.GetCardsAsync(productIds, ct);
        
        var cardsMap = dbCards.ToDictionary(c => c.Id);
        
        var sortedCards = productIds
            .Where(id => cardsMap.ContainsKey(id))
            .Select(id => cardsMap[id])
            .ToList();
        
        return new ProductCardsPage(sortedCards, dbPagedResult.NextCursor, filter.PageSize);
    }

    public async Task<ProductDetailsDto> GetProductAsync(Guid id, CancellationToken ct = default)
    {
        var dbDetails = await queryRepository.GetDetailsAsync(id, ct);

        if (dbDetails == null)
        {
            throw new NotFoundException(nameof(Product), id);
        }
        
        return dbDetails;
    }

    private static bool TryGetSku(string? search, out long sku)
    {
        sku = 0;
        if (string.IsNullOrWhiteSpace(search))
        {
            return false;
        }
        
        var trimmedSearch = search.Trim();
        return long.TryParse(trimmedSearch, out sku) && sku > 0;
    }
}
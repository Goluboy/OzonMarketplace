using ProductService.Application.DTO.Product;
using ProductService.Application.Exceptions;
using ProductService.Application.Helpers;
using ProductService.Domain.Entities;
using ProductService.Infrastructure.Abstractions.DTO.Product.Query;
using ProductService.Infrastructure.Abstractions.Repository.Abstractions.Products;

namespace ProductService.Application.Services.Products.Query;

public class ProductQueryService(IProductQueryRepository queryRepository, IProductImageUrlHelper urlHelper) : IProductQueryService
{
    public async Task<ProductCardsPage> GetCatalogAsync(ProductSearchFilter filter, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        
        if (TryGetSku(filter.Search, out var sku))
        {
            var skuCards = await queryRepository.GetCardsAsync(sku);
            
            var formattedSkuCards = skuCards
                .Select(dto => dto with
                {
                    MainImageUrl = urlHelper.ToAbsoluteUrl(dto.MainImageUrl)
                })
                .ToList();
            
            return new ProductCardsPage(formattedSkuCards, NextCursor: null, filter.PageSize);
        }
        
        var dbPagedResult = await queryRepository.GetPagedAsync(filter);
        
        if (!dbPagedResult.ProductIds.Any())
        {
            return new ProductCardsPage([], null, filter.PageSize);
        }
        
        var productIds = dbPagedResult.ProductIds;
        
        var dbCards = await queryRepository.GetCardsAsync(productIds);
        
        var cardsMap = dbCards.ToDictionary(c => c.Id);
        
        var sortedCards = productIds
            .Where(id => cardsMap.ContainsKey(id))
            .Select(id => cardsMap[id])
            .Select(dto => dto with
            {
                MainImageUrl = urlHelper.ToAbsoluteUrl(dto.MainImageUrl)
            })
            .ToList();
        
        return new ProductCardsPage(sortedCards, dbPagedResult.NextCursor, filter.PageSize);
    }

    public async Task<ProductDetailsDto> GetProductAsync(Guid id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        
        var dbDetails = await queryRepository.GetDetailsAsync(id);

        if (dbDetails == null)
        {
            throw new NotFoundException(nameof(Product), id);
        }

        var dto = dbDetails with
        {
            Images = urlHelper.ToAbsoluteImageDtos(dbDetails.Images.ToList())
        };
        return dto;
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
using System.Globalization;
using ProductService.Application.DTO.Product;
using ProductService.Infrastructure.Abstractions.DTO.Product.Query;
using ProductService.Presentation.Models;
using MoneyDto = ProductService.Presentation.Models.MoneyDto;
using ProductImageDto = ProductService.Presentation.Models.ProductImageDto;

namespace ProductService.Presentation.Mappers;

public static class ProductMapper
{
    public static ProductSearchFilter ToDto(this ProductSearchFilterRequest request)
    {
        return new ProductSearchFilter(
            request.Search,
            request.CategoryId,
            request.MinPrice?.ToDto(),
            request.MaxPrice?.ToDto(),
            request.SortBy,
            request.SortOrder,
            request.Cursor,
            request.PageSize);
    }

    public static CreateProductDto ToDto(this CreateProductRequest request)
    {
        return new CreateProductDto(
            request.Sku,
            request.Name,
            request.Description,
            request.Price.ToDto(),
            request.CategoryId,
            request.Images.Select(url => url.Url).ToList());
    }
    
    public static UpdateProductDto ToDto(this UpdateProductRequest request, Guid productId)
    {
        return new UpdateProductDto(
            productId,
            request.Name,
            request.Description,
            request.Price.ToDto(),
            request.CategoryId,
            request.Images.Select(url => url.Url).ToList());
    }

    public static ProductResponse ToHttpResponse(this ProductDetailsDto dto)
    {
        return new ProductResponse(
            dto.Id,
            dto.Sku,
            dto.SellerId,
            dto.Name,
            dto.Description,
            new MoneyDto(dto.PriceAmount.ToString(CultureInfo.InvariantCulture), dto.PriceCurrency),
            dto.CategoryId,
            dto.CategoryName,
            dto.CategoryPath,
            dto.Images.Select(imageDto => imageDto.ToHttpResponse()).ToList(),
            dto.CreatedAt,
            dto.UpdatedAt);
    }
    
    public static ProductCursorPagedResponse ToHttpResponse(this ProductCardsPage dto)
    {
        return new ProductCursorPagedResponse(
            dto.Items.Select(i => i.ToHttpResponse()).ToList(),
            dto.NextCursor,
            dto.PageSize);
    }

    public static ProductImageDto ToHttpResponse(this Infrastructure.Abstractions.DTO.Product.Query.ProductImageDto dto)
    {
        return new ProductImageDto(dto.Url);
    }
    
    public static Infrastructure.Abstractions.DTO.Product.Query.ProductImageDto ToDto(this ProductImageDto dto)
    {
        return new Infrastructure.Abstractions.DTO.Product.Query.ProductImageDto(dto.Url);
    }
    
    private static ProductCardResponse ToHttpResponse(this ProductCardDto dto)
    {
        return new ProductCardResponse(
            dto.Id,
            dto.Name,
            new MoneyDto(dto.PriceAmount.ToString(CultureInfo.InvariantCulture), dto.PriceCurrency),
            dto.MainImageUrl,
            dto.CategoryId);
    }
    
    private static Infrastructure.Abstractions.DTO.Product.Query.MoneyDto ToDto(this MoneyDto dto)
    {
        var amount = decimal.Parse(dto.Amount, CultureInfo.InvariantCulture);
        
        return new Infrastructure.Abstractions.DTO.Product.Query.MoneyDto(amount, dto.Currency);
    }
}
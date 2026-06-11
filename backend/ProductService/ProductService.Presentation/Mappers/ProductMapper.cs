using System.Globalization;
using ProductService.Application.DTO.Product;
using ProductService.Infrastructure.Abstractions.DTO.Product.Query;
using ProductService.Presentation.Models;

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
            request.Images.Select(img => img.ToDto()).ToList());
    }
    
    public static UpdateProductDto ToDto(this UpdateProductRequest request, Guid productId)
    {
        return new UpdateProductDto(
            productId,
            request.Name,
            request.Description,
            request.Price.ToDto(),
            request.CategoryId,
            request.Images.Select(img => img.ToDto()).ToList());
    }

    public static ProductResponse ToHttpResponse(this ProductDetailsDto dto)
    {
        return new ProductResponse(
            dto.Id,
            dto.Sku,
            dto.SellerId,
            dto.Name,
            dto.Description,
            new MoneyHttpDto(dto.PriceAmount.ToString(CultureInfo.InvariantCulture), dto.PriceCurrency),
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

    public static ProductImageHttpDto ToHttpResponse(this ProductImageDto dto)
    {
        return new ProductImageHttpDto(dto.Url);
    }
    
    public static ProductImageDto ToDto(this ProductImageHttpDto httpDto)
    {
        return new ProductImageDto(httpDto.Url);
    }
    
    private static ProductCardResponse ToHttpResponse(this ProductCardDto dto)
    {
        return new ProductCardResponse(
            dto.Id,
            dto.Name,
            new MoneyHttpDto(dto.PriceAmount.ToString(CultureInfo.InvariantCulture), dto.PriceCurrency),
            dto.MainImageUrl,
            dto.CategoryId);
    }
    
    private static MoneyDto ToDto(this MoneyHttpDto httpDto)
    {
        var amount = decimal.Parse(httpDto.Amount, CultureInfo.InvariantCulture);
        
        return new MoneyDto(amount, httpDto.Currency);
    }
}
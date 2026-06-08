using ProductService.Application.DTO.Category;
using ProductService.Domain.Entities;
using ProductService.Infrastructure.Abstractions.DTO.Product.Query;

namespace ProductService.Application.Mappers;

public static class ProductMapper
{
    public static ProductDetailsDto ToDto(this Product product, CategoryDto categoryDto)
    {
        return new ProductDetailsDto
        {
            Id = product.Id, 
            Sku = product.Sku,
            Name = product.Name,
            Description = product.Description,
            SellerId = product.SellerId,
            PriceAmount = product.Price.Amount,
            PriceCurrency = product.Price.Currency,
            CategoryId = product.CategoryId,
            CategoryName = categoryDto.Name,
            CategoryPath = categoryDto.Path,
            Images = product.Images.Select(image => image.Url).ToList(),
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        };
    }
}
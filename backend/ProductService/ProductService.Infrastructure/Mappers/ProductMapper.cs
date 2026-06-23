using ProductService.Domain.Entities;
using ProductService.Domain.ValueObjects;
using ProductService.Infrastructure.Abstractions.DTO.Product.Query;
using ProductService.Infrastructure.DAO;

namespace ProductService.Infrastructure.Mappers;

public static class ProductMapper
{
    public static Product ToDomain(this ProductDao dao)
    {
        var domainImages = dao.Images
            .Select(img => new ProductImage(img.Url))
            .ToList();
        
        return Product.Reconstruct(
            id: dao.Id,
            sellerId: dao.SellerId,
            sku: dao.Sku,
            name: dao.Name,
            description: dao.Description,
            price: new Money(dao.PriceAmount, dao.PriceCurrency),
            categoryId: dao.CategoryId,
            createdAt: dao.CreatedAt,
            updatedAt: dao.UpdatedAt,
            version: dao.Version,
            images: domainImages);
    }

    public static ProductDao ToDao(this Product product)
    {
        var daoImages =  product.Images
            .Select(img => new ProductImageDto(img.Url))
            .ToList();

        return new ProductDao
        (
            Id: product.Id,
            SellerId: product.SellerId,
            Sku: product.Sku,
            Name: product.Name,
            Description: product.Description,
            PriceAmount: product.Price.Amount,
            PriceCurrency: product.Price.Currency,
            CategoryId: product.CategoryId,
            CreatedAt: product.CreatedAt,
            UpdatedAt: product.UpdatedAt,
            Version: product.Version,
            Images: daoImages
        );
    }
}
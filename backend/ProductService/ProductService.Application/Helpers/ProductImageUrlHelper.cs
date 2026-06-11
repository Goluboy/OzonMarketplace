using Core.Minio.Helpers;
using ProductService.Domain.ValueObjects;
using ProductService.Infrastructure.Abstractions.DTO.Product.Query;

namespace ProductService.Application.Helpers;

public class ProductImageUrlHelper(IS3UrlFormatter formatter) : IProductImageUrlHelper
{
    public List<ProductImage> ToStoredImages(IEnumerable<ProductImageDto> absoluteUrls)
    {
        var productImageDtos = absoluteUrls as List<ProductImageDto> ?? absoluteUrls.ToList();
        if (productImageDtos.Count == 0)
        {
            return [];
        }
        
        return productImageDtos
            .Select(url => new ProductImage(formatter.ToObjectKey(url.Url)))
            .ToList();
    }

    public List<ProductImageDto> ToAbsoluteImageDtos(IEnumerable<ProductImageDto> images)
    {
        var productImageDtos = images as List<ProductImageDto> ?? images.ToList();
        if (productImageDtos.Count == 0)
        {
            return [];
        }
        
        return productImageDtos
            .Select(image => new ProductImageDto(formatter.ToAbsoluteUrl(image.Url)))
            .ToList();
    }

    public string ToStoredUrl(string absoluteUrl)
    {
        return formatter.ToObjectKey(absoluteUrl);
    }

    public string ToAbsoluteUrl(string storedUrl)
    {
        return formatter.ToAbsoluteUrl(storedUrl);
    }
}
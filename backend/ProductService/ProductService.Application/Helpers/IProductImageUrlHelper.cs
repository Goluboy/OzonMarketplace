using ProductService.Domain.ValueObjects;
using ProductService.Infrastructure.Abstractions.DTO.Product.Query;

namespace ProductService.Application.Helpers;

public interface IProductImageUrlHelper
{
    List<ProductImage> ToStoredImages(IEnumerable<ProductImageDto> absoluteUrls);
    List<ProductImageDto> ToAbsoluteImageDtos(IEnumerable<ProductImageDto> images);
    string ToStoredUrl(string absoluteUrl);
    string ToAbsoluteUrl(string storedUrl);
}
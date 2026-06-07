using ProductService.Application.DTO.Product;
using ProductService.Infrastructure.Abstractions.DTO.Product.Query;

namespace ProductService.Application.Services.Products;

public interface IProductQueryService
{
    Task<ProductCardsPage> GetCatalogAsync(ProductSearchFilter filter, CancellationToken ct = default);
    Task<ProductDetailsDto> GetProductAsync(Guid id, CancellationToken ct = default);
}
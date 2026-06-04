using ProductService.Infrastructure.Abstractions.DTO.Product.Query;

namespace ProductService.Infrastructure.Abstractions.Repository.Abstractions.Product;

public interface IProductQueryRepository
{
    Task<IReadOnlyList<ProductCardDto>> GetCardsBySkuAsync(long sku);
    Task<ProductCardDto?> GetCardByIdAsync(Guid id);
    Task<ProductDetailsDto?> GetDetailsByIdAsync(Guid id);
    Task<ProductPagedIdsDto> GetPagedAsync(ProductSearchFilter filter);
}
using ProductService.Infrastructure.Abstractions.DTO.Product.Query;

namespace ProductService.Infrastructure.Abstractions.Repository.Abstractions.Products;

public interface IProductQueryRepository
{
    Task<IReadOnlyList<ProductCardDto>> GetCardsAsync(long sku);
    Task<IReadOnlyList<ProductCardDto>> GetCardsAsync(IReadOnlyList<Guid> ids);
    Task<ProductCardDto?> GetCardAsync(Guid id);
    Task<ProductDetailsDto?> GetDetailsAsync(Guid id);
    Task<ProductPagedIdsDto> GetPagedAsync(ProductSearchFilter filter);
}
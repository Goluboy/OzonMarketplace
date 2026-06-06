using ProductService.Infrastructure.Abstractions.DTO.Product.Query;

namespace ProductService.Infrastructure.Abstractions.Repository.Abstractions.Products;

public interface IProductQueryRepository
{
    Task<IReadOnlyList<ProductCardDto>> GetCardsAsync(long sku, CancellationToken ct = default);
    Task<IReadOnlyList<ProductCardDto>> GetCardsAsync(IReadOnlyList<Guid> ids, CancellationToken ct = default);
    Task<ProductCardDto?> GetCardAsync(Guid id, CancellationToken ct = default);
    Task<ProductDetailsDto?> GetDetailsAsync(Guid id, CancellationToken ct = default);
    Task<ProductPagedIdsDto> GetPagedAsync(ProductSearchFilter filter, CancellationToken ct = default);
}
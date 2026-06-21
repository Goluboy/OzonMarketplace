using ProductService.Infrastructure.Abstractions.DTO.Product.Query;

namespace ProductService.Infrastructure.DAO;

public record CategoryDao(
    int Id,
    string Name,
    string Path);

public record ProductDao(
    Guid Id,
    Guid SellerId,
    long Sku,
    string Name,
    string Description,
    decimal PriceAmount,
    string PriceCurrency,
    int CategoryId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int Version,
    List<ProductImageDto> Images)
{
    public ProductDao() : this(
        Guid.Empty, Guid.Empty, 0, null!, null!, 0,
        null!, 0, default, default, 0, []
    ) {}
}
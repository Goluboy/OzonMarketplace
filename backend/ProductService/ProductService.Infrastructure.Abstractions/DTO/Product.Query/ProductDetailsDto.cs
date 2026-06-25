
namespace ProductService.Infrastructure.Abstractions.DTO.Product.Query;

public record ProductDetailsDto(
    Guid Id,
    long Sku,
    Guid SellerId,
    string Name,
    string Description,
    decimal PriceAmount,
    string PriceCurrency,
    int CategoryId,
    string CategoryName,
    string CategoryPath,
    List<ProductImageDto> Images,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public ProductDetailsDto() : this(
        Guid.Empty, 0, Guid.Empty, null!, null!, 0, null!,
        0, null!, null!, null!, default, default) {}
}
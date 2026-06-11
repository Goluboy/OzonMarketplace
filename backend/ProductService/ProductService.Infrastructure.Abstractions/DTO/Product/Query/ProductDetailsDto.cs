namespace ProductService.Infrastructure.Abstractions.DTO.Product.Query;

public record ProductDetailsDto
{
    public ProductDetailsDto() {}
    public Guid Id { get; init; }
    public long Sku { get; init; }
    public Guid SellerId { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public decimal PriceAmount { get; init; }
    public required string PriceCurrency { get; init; }
    public int CategoryId { get; init; }
    public required string CategoryName { get; init; }
    public required string CategoryPath { get; init; }
    public List<ProductImageDto> Images { get; init; } = [];
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}
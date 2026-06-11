namespace ProductService.Infrastructure.Abstractions.DTO.Product.Query;

public record ProductCardDto
{
    public ProductCardDto(){}
    public Guid Id { get; init; }
    public Guid SellerId { get; init; }
    public int CategoryId { get; init; }
    public required string Name { get; init; }
    public decimal PriceAmount { get; init; }
    public required string PriceCurrency { get; init; }
    public required string MainImageUrl { get; init; }
}
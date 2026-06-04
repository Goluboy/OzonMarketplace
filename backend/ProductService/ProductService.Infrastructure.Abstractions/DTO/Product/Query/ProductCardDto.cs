namespace ProductService.Infrastructure.Abstractions.DTO.Product.Query;

public record ProductCardDto(
    Guid Id,
    int CategoryId,
    string Name,
    decimal PriceAmount,
    string PriceCurrency,
    string MainImageUrl);
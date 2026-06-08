namespace ProductService.Infrastructure.Abstractions.DTO.Product.Query;

public record ProductCardDto(
    Guid Id,
    Guid SellerId,
    int CategoryId,
    string Name,
    decimal PriceAmount,
    string PriceCurrency,
    string MainImageUrl);
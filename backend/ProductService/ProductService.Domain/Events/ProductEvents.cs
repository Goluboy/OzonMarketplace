namespace ProductService.Domain.Events;

public record ProductCreatedEvent(
    Guid ProductId,
    Guid SellerId,
    long Sku,
    string Name,
    decimal PriceAmount,
    string Currency,
    int CategoryId,
    List<string> ImageUrls) : DomainEvent;
    
public record ProductPriceChangedEvent(
    Guid ProductId,
    decimal OldAmount,
    string OldCurrency,
    decimal NewAmount,
    string NewCurrency) : DomainEvent;
    
public record ProductDetailsUpdatedEvent(
    Guid ProductId,
    string NewName,
    string NewDescription,
    int NewCategoryId) : DomainEvent;

public record ProductImagesUpdatedEvent(
    Guid ProductId,
    List<string> ImageUrls) : DomainEvent;
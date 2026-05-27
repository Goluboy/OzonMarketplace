namespace ProductService.Domain.Events;

public record ProductCreatedEvent(
    Guid ProductId,
    int Sku,
    string Name,
    decimal PriceAmount,
    Guid CategoryId,
    List<string> ImageUrls) : DomainEvent;
    
public record ProductPriceChangedEvent(
    Guid ProductId,
    decimal OldAmount,
    decimal NewAmount) : DomainEvent;
    
public record ProductDetailsUpdatedEvent(
    Guid ProductId,
    string NewName,
    string NewDescription,
    Guid NewCategoryId) : DomainEvent;

public record ProductImagesUpdatedEvent(
    Guid ProductId,
    List<string> ImageUrls) : DomainEvent;
using ProductService.Domain.Entities;

namespace ProductService.Domain.Events;

public record CategoryCreatedEvent(
    Category Category) : DomainEvent;
    
public record CategoryRenamedEvent(
    int CategoryId,
    string NewName) : DomainEvent;
    
public record CategoryPathChangedEvent(
    int CategoryId,
    string NewPath) : DomainEvent;
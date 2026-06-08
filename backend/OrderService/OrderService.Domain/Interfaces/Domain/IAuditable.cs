namespace OrderService.Domain.Interfaces.Domain;

public interface IAuditable
{
    DateTime CreatedAt { get; }
    DateTime? UpdatedAt { get; }
}
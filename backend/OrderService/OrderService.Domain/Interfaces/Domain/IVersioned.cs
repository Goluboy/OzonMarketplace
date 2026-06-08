namespace OrderService.Domain.Interfaces.Domain;

public interface IVersioned
{
    int Version { get; }
    void IncrementVersion();
}
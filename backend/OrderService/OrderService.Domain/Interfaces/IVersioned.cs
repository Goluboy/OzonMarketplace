namespace OrderService.Domain.Interfaces;

public interface IVersioned
{
    int Version { get; }
    void IncrementVersion();
}
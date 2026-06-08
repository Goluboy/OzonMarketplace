namespace OrderService.Domain.Interfaces.Persistence;

public interface IUnitOfWork : IDisposable
{
    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);

    System.Data.IDbTransaction? CurrentTransaction { get; }
}

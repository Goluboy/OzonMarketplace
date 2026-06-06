using System.Data;
using System.Data.Common;

namespace ProductService.Infrastructure.Abstractions.UnitOfWork.Abstractions;

public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task BeginOutboxTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync();
}
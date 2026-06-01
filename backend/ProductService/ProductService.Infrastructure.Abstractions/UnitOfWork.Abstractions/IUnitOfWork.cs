using System.Data;
using System.Data.Common;

namespace ProductService.Infrastructure.Abstractions.UnitOfWork.Abstractions;

public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    Task<DbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task<DbTransaction> BeginOutboxTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
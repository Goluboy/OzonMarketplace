using System.Data.Common;

namespace OrderService.Infrastructure.Persistence.UnitOfWork;

public interface IDbSession : IDisposable, IAsyncDisposable
{
    DbConnection Connection { get; }
    DbTransaction? Transaction { get; }
}
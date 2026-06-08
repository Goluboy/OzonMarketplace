using System.Data.Common;

namespace ProductService.Infrastructure.UnitOfWork;

public interface IDbSession : IDisposable, IAsyncDisposable
{
    DbConnection Connection { get; }
    DbTransaction? Transaction { get; }
}
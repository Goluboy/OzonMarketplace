using System.Data.Common;

namespace ProductService.Infrastructure.UnitOfWork;

internal interface IDbSession : IDisposable, IAsyncDisposable
{
    DbConnection Connection { get; }
    DbTransaction? Transaction { get; }
}
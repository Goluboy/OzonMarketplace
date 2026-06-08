using System.Data;
using OrderService.Domain.Interfaces.Persistence;

namespace OrderService.Infrastructure.Persistence;

public class UnitOfWork(IDbConnection connection) : IUnitOfWork
{
    public IDbConnection Connection => connection;
    private IDbTransaction? _transaction;

    public IDbTransaction? CurrentTransaction => _transaction;

    public async Task BeginTransactionAsync(CancellationToken ct = default)
    {
        if (connection.State != ConnectionState.Open)
        {
            if (connection is System.Data.Common.DbConnection dbConn)
            {
                await dbConn.OpenAsync(ct);
            }
            else
            {
                connection.Open();
            }
        }

        _transaction = connection.BeginTransaction();
    }

    public async Task CommitAsync(CancellationToken ct = default)
    {
        if (_transaction == null)
            throw new InvalidOperationException("No transaction has been started. Call BeginTransactionAsync first.");

        try
        {
            _transaction.Commit();
        }
        finally
        {
            _transaction.Dispose();
            _transaction = null;
        }
    }

    public async Task RollbackAsync(CancellationToken ct = default)
    {
        if (_transaction == null)
            return;

        try
        {
            _transaction.Rollback();
        }
        finally
        {
            _transaction.Dispose();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
    }
}

using System.Data;
using System.Data.Common;
using DotNetCore.CAP;
using Npgsql;
using ProductService.Infrastructure.Abstractions.UnitOfWork.Abstractions;
using ProductService.Infrastructure.Persistence.Provider;

namespace ProductService.Infrastructure.UnitOfWork;

public class UnitOfWork(IPostgresConnectionFactory connectionFactory, ICapPublisher capPublisher)
    : IUnitOfWork, IDbSession
{
    private DbConnection? _connection;

    public DbConnection Connection => _connection ??= connectionFactory.GetConnection();
    public DbTransaction? Transaction { get; private set; }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (Transaction != null)
        {
            return;
        }
        
        await EnsureConnectionOpenAsync(cancellationToken);
        
        Transaction = await Connection.BeginTransactionAsync(cancellationToken); ;
    }

    public async Task BeginOutboxTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (Transaction != null)
        {
            return ;
        }

        await EnsureConnectionOpenAsync(cancellationToken);
        
        var npgsqlConnection = (NpgsqlConnection)Connection;
        var capTransaction = await npgsqlConnection.BeginTransactionAsync(capPublisher, autoCommit: false, cancellationToken);
        
        Transaction = (DbTransaction?)capTransaction.DbTransaction 
                       ?? throw new InvalidOperationException("Transaction CAP is not open.");
    }

    public async Task CommitAsync()
    {
        if (Transaction == null)
        {
            throw new InvalidOperationException("Transaction is not open.");
        }
        
        await Transaction.CommitAsync(CancellationToken.None);
        
        if (Transaction != null)
        {
            await Transaction.DisposeAsync();
            Transaction = null;
        }
    }

    public async Task RollbackAsync()
    {
        if (Transaction != null)
        {
            await Transaction.RollbackAsync(CancellationToken.None);
            await Transaction.DisposeAsync();
            Transaction = null;
        }
    }

    private async Task EnsureConnectionOpenAsync(CancellationToken cancellationToken)
    {
        if (Connection.State != ConnectionState.Open)
        {
            await Connection.OpenAsync(cancellationToken);
        }
    }
    
    public void Dispose()
    {
        Transaction?.Dispose();
        _connection?.Dispose();
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        if (Transaction != null)
        {
            await Transaction.DisposeAsync();
        }

        if (_connection != null)
        {
            await _connection.DisposeAsync();
        }
        
        GC.SuppressFinalize(this);
    }
}
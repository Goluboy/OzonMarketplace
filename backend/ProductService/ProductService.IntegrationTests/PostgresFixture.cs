

using Dapper;
using DotNetCore.CAP;
using Microsoft.Extensions.Configuration;
using Npgsql;
using ProductService.Infrastructure.Abstractions.Repository.Abstractions;
using ProductService.Infrastructure.Abstractions.Repository.Abstractions.Products;
using ProductService.Infrastructure.Abstractions.UnitOfWork.Abstractions;
using ProductService.Infrastructure.Persistence;
using ProductService.Infrastructure.Persistence.Provider;
using ProductService.Infrastructure.Repository;
using ProductService.Infrastructure.Repository.Products;
using ProductService.Infrastructure.UnitOfWork;
using Testcontainers.PostgreSql;

namespace ProductService.IntegrationTests;

public sealed class PostgresFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private IConfiguration? _configuration;

    public string ConnectionString => _container?.GetConnectionString() ?? string.Empty;
    
    [Obsolete("Obsolete")]
    public async Task InitializeAsync()
    {
        if (_container != null)
        {
            return;
        }
        
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .WithDatabase("product_service_test")
            .WithUsername("admin")
            .WithPassword("admin")
            .WithCleanUp(true)
            .Build();

        await _container.StartAsync();

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PostgresConnectionString"] = _container.GetConnectionString()
            })
            .Build();
        
        _configuration.RunMigrations();
        
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }
    
    public async Task ResetAsync(string table)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        

        await connection.ExecuteAsync($"TRUNCATE {table} RESTART IDENTITY CASCADE;");
    }

    public IPostgresConnectionFactory CreateConnectionFactory()
    {
        if (_configuration is null)
        {
            throw new InvalidOperationException("PostgresFixture has not been initialized.");
        }

        return new PostgresConnectionFactory(_configuration);
    }
    
    public (IUnitOfWork UnitOfWork, IDbSession DbSession) CreateUnitOfWorkContext()
    {
        var connectionFactory = CreateConnectionFactory();
        var fakeCapPublisher = new FakeCapPublisher();
        
        var uow = new UnitOfWork(connectionFactory, fakeCapPublisher);
        
        return (uow, uow);
    }
    

    public ICategoryRepository CreateCategoryRepository(IDbSession dbSession)
    {
        return new CategoryRepository(dbSession);
    }

    public IProductQueryRepository CreateProductQueryRepository(IPostgresConnectionFactory connectionFactory)
    {
        return new ProductQueryRepository(connectionFactory);
    }

    public IProductRepository CreateProductRepository(IDbSession dbSession)
    {
        return new ProductRepository(dbSession);
    }

    public async Task DisposeAsync()
    {
        if (_container is null)
        {
            return;
        }

        await _container.DisposeAsync();
        _container = null;
        _configuration = null;
    }
    
    private sealed class FakeCapPublisher : ICapPublisher
    {
        private readonly AsyncLocal<ICapTransaction?> _transaction = new();
        public IServiceProvider ServiceProvider => throw new NotImplementedException();
        
        public ICapTransaction? Transaction
        {
            get => _transaction.Value;
            set => _transaction.Value = value;
        }
        
        public Task PublishAsync<T>(string name, T? value, string? callbackName = null, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task PublishAsync<T>(string name, T? value, IDictionary<string, string?> headers, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public void Publish<T>(string name, T? value, string? callbackName = null) { }
        public void Publish<T>(string name, T? value, IDictionary<string, string?> headers) { }
        
        public void PublishDelay<T>(TimeSpan delay, string name, T? value, IDictionary<string, string?> headers) { }

        public void PublishDelay<T>(TimeSpan delay, string name, T? value, string? callbackName = null) { }

        public Task PublishDelayAsync<T>(TimeSpan delay, string name, T? value, IDictionary<string, string?> headers, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task PublishDelayAsync<T>(TimeSpan delay, string name, T? value, string? callbackName = null, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
using System.Data;
using Npgsql;
using Testcontainers.PostgreSql;
using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;
using OrderService.Infrastructure.Persistence.Migrations;

namespace OrderService.Infrastructure.Persistence.Tests.Fixtures;

public class PostgreSqlFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private NpgsqlConnection? _connection;
    private const string DbName = "orderservice_test";
    private const string DbUser = "postgres";
    private const string DbPassword = "postgres";

    public IDbConnection Connection
    {
        get => _connection ?? throw new InvalidOperationException("Connection not initialized. Call InitializeAsync first.");
    }

    public string ConnectionString
    {
        get
        {
            if (_container == null)
                throw new InvalidOperationException("Container not initialized. Call InitializeAsync first.");

            return _container.GetConnectionString();
        }
    }

    public async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase(DbName)
            .WithUsername(DbUser)
            .WithPassword(DbPassword)
            .WithCleanUp(true)
            .Build();

        await _container.StartAsync();

        _connection = new NpgsqlConnection(_container.GetConnectionString());

        await _connection.OpenAsync();

        await RunMigrationsAsync();
    }

    public async Task DisposeAsync()
    {
        if (_connection != null)
        {
            await _connection.CloseAsync();
            await _connection.DisposeAsync();
        }

        if (_container != null)
        {
            await _container.StopAsync();
            await _container.DisposeAsync();
        }
    }

    private async Task RunMigrationsAsync()
    {
        var services = new ServiceCollection()
            .AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddPostgres()
                .WithGlobalConnectionString(ConnectionString)
                .ScanIn(typeof(CreateInitialOrderSchema).Assembly))
            .BuildServiceProvider();

        var runner = services.GetRequiredService<IMigrationRunner>();
        runner.MigrateUp();

        await services.DisposeAsync();
    }

    /// <summary>
    /// Clears all data from the tables while preserving schema.
    /// </summary>
    public async Task ClearDatabaseAsync()
    {
        using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            TRUNCATE TABLE "OrderStatusHistories" CASCADE;
            TRUNCATE TABLE "OrderItems" CASCADE;
            TRUNCATE TABLE "Orders" CASCADE;
        """;
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Gets a new connection for independent operations.
    /// </summary>
    public IDbConnection GetConnection()
    {
        return new NpgsqlConnection(ConnectionString);
    }
}

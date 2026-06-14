using FluentMigrator.Runner;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderService.Domain.Interfaces.Persistence;
using OrderService.Infrastructure.Persistence.Provider;
using OrderService.Infrastructure.Persistence.Repositories;
using OrderService.Infrastructure.Persistence.UnitOfWork;

namespace OrderService.Infrastructure.Persistence;

public static class PersistenceDependencyInjection
{
    public static IServiceCollection AddPersistenceServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
                               ?? throw new InvalidOperationException("DefaultConnection string is missing.");

        services.AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddPostgres()
                .WithGlobalConnectionString(connectionString)
                .ScanIn(typeof(PersistenceDependencyInjection).Assembly).For.Migrations())
            .AddLogging(lb => lb.AddFluentMigratorConsole());

        services.AddCap(x =>
        {
            x.UsePostgreSql(options =>
            {
                options.ConnectionString = configuration.GetConnectionString("DefaultConnection");
            });

            x.UseKafka(options =>
            {
                options.Servers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
            });

            x.FailedRetryCount = 3;
            x.FailedRetryInterval = 60;
        });

        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork.UnitOfWork>();
        services.AddScoped<IDbSession, UnitOfWork.UnitOfWork>();
        services.AddScoped<IPostgresConnectionFactory, PostgresConnectionFactory>();
        services.AddScoped<IProcessedEventsRepository, ProcessedEventsRepository>();

        return services;
    }
}
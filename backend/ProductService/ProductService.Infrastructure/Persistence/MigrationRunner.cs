using FluentMigrator.Runner;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ProductService.Infrastructure.Persistence;

public static class MigrationRunner
{
    public static void RunMigrations(this IConfiguration configuration)
    {
        var postgresConnString = configuration.GetConnectionString("PostgresConnectionString")
                                 ?? throw new NullReferenceException("Connection string 'PostgresConnectionString' not found.");
        
        var serviceContext = CreateService(postgresConnString);
        using var scope = serviceContext.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
        runner.MigrateUp();
    }

    private static IServiceProvider CreateService(string connectionString)
        => new ServiceCollection()
            .AddFluentMigratorCore()
            .ConfigureRunner(builder => builder
                .AddPostgres()
                .WithGlobalConnectionString(connectionString)
                .ScanIn(typeof(MigrationRunner).Assembly).For.Migrations()
                .ConfigureGlobalProcessorOptions(op => op.ProviderSwitches = "Force Quote=false"))
            .AddLogging(log => log.AddFluentMigratorConsole())
            .BuildServiceProvider(false);
}
using FluentMigrator.Runner;
using static MassTransit.MessageHeaders;

namespace OrderService.Http
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            // === АВТОМАТИЧЕСКИЙ ЗАПУСК МИГРАЦИЙ ===
            await RunMigrationsAsync(host);

            // Запуск приложения
            await host.RunAsync();
        }

        private static async Task RunMigrationsAsync(IHost host)
        {
            using var scope = host.Services.CreateScope();
            var services = scope.ServiceProvider;
            var logger = services.GetRequiredService<ILogger<Program>>();

            try
            {
                logger.LogInformation("🔄 Starting database migrations...");

                var runner = services.GetRequiredService<IMigrationRunner>();

                // Применяем все непримененные миграции
                runner.MigrateUp();

                logger.LogInformation("✅ All migrations completed successfully!");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ An error occurred while migrating the database");

                // В Production: падаем, если миграции не прошли!
                // Лучше упасть на старте, чем работать с некорректной схемой БД
                throw;
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
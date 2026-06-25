using DotNetCore.CAP;
using DotNetCore.CAP.Filter;
using DotNetCore.CAP.Messages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderService.Infrastructure.EventBus.Consumers;
using OrderService.Infrastructure.EventBus.Dispatcher;

namespace OrderService.Infrastructure.EventBus;

public static class CapServiceCollectionExtensions
{
    public static IServiceCollection AddCapServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("DefaultConnection is missing");
        
        var kafkaServers = configuration["KafkaSettings:Servers"] 
            ?? "localhost:29092";

        services.AddCap(x =>
        {

            x.UsePostgreSql(opt =>
            {
                opt.ConnectionString = connectionString;
                opt.Schema = "cap";
            });

            x.UseKafka(kafkaServers);

            x.FailedRetryCount = 5;
            x.FailedRetryInterval = 60;

            x.SucceedMessageExpiredAfter = 24 * 3600;

            x.UseDashboard(opt =>
            {
                opt.PathMatch = "/cap";
            });

            x.DefaultGroupName = "order-service-group";
            x.GroupNamePrefix = "order-service";

        });

        services.AddScoped<OrderEventDispatcher>();
        services.AddScoped<ProductsEventDispatcher>();
        services.AddScoped<PricesEventDispatcher>();

        services.AddScoped<OrderCreatedConsumer>();
        services.AddScoped<OrderSagaTimeoutConsumer>();
        services.AddScoped<OrderCancelledConsumer>();
        services.AddScoped<StockReservedConsumer>();
        services.AddScoped<StockReservationFailedConsumer>();
        services.AddScoped<PriceCalculatedConsumer>();

        return services;
    }
}
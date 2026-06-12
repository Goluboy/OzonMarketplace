using IntegrationEvents.IntegrationEvents;
using IntegrationEvents.Shared;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderService.Infrastructure.EventBus.Consumers;
using Quartz;

namespace OrderService.Infrastructure.EventBus;

public static class KafkaDependencyInjection
{
    public static void AddKafkaIntegration(this IServiceCollection services, IConfiguration config)
    {
        var connectionString = config.GetConnectionString("DefaultConnection");

        services.AddQuartz(q =>
        {
            q.SchedulerName = "OrderService-Scheduler";

            q.UseMicrosoftDependencyInjectionJobFactory();

            q.UsePersistentStore(s =>
            {
                s.UsePostgres(connectionString!);
                s.UseSystemTextJsonSerializer();

                s.UseClustering(c =>
                {
                    c.CheckinMisfireThreshold = TimeSpan.FromSeconds(20);
                    c.CheckinInterval = TimeSpan.FromSeconds(10);
                });
            });
        });

        services.AddMassTransit(x =>
        {
            x.AddPublishMessageScheduler();
            x.AddQuartzConsumers();

            x.AddRider(rider =>
            {
                rider.AddConsumer<StockReservedConsumer>();
                rider.AddConsumer<StockReservationFailedConsumer>();
                rider.AddConsumer<PriceCalculatedConsumer>();
                rider.AddConsumer<OrderSagaTimeoutConsumer>();

                rider.UsingKafka((context, k) =>
                {
                    k.Host("localhost:29092");

                    k.TopicEndpoint<StockReservedEvent>("products.stock.reserved", "order-service-stock-group", cfg =>
                    {
                        cfg.CreateIfMissing();
                        cfg.ConfigureConsumer<StockReservedConsumer>(context);
                        cfg.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
                    });

                    k.TopicEndpoint<StockReservationFailedEvent>("products.stock.failed", "order-service-failed-group", cfg =>
                    {
                        cfg.CreateIfMissing();
                        cfg.ConfigureConsumer<StockReservationFailedConsumer>(context);
                        cfg.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
                    });

                    k.TopicEndpoint<PriceCalculatedEvent>("prices.calculated", "order-service-price-group", cfg =>
                    {
                        cfg.CreateIfMissing();
                        cfg.ConfigureConsumer<PriceCalculatedConsumer>(context);
                        cfg.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
                    });

                    k.TopicEndpoint<OrderSagaTimeout>("order-service-timeout", "order-service-timeout-group", cfg =>
                    {
                        cfg.CreateIfMissing();
                        cfg.ConfigureConsumer<OrderSagaTimeoutConsumer>(context);

                        cfg.UsePublishMessageScheduler();
                    });
                });

                rider.AddProducer<Guid, OrderCreatedEvent>("orders.created");
                rider.AddProducer<Guid, OrderCancelledEvent>("orders.cancelled");
            });

            x.UsingInMemory((context, cfg) =>
            {
                cfg.ConfigureEndpoints(context);
            });
        });

        services.AddScoped<IPublishEndpoint>(sp => sp.GetRequiredService<IBus>());
    }
}

using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderService.Application.Commands.Common;
using OrderService.Infrastructure.EventBus.Consumers;
using OrderService.IntegrationEvents.IntegrationEvents;

namespace OrderService.Infrastructure.EventBus;

public static class KafkaDependencyInjection
{
    public static void AddKafkaIntegration(this IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<IEventPublisher, MassTransitEventPublisher>();

        services.AddMassTransit(x =>
        {
            x.AddRider(rider =>
            {
                // === Регистрация консюмеров ===
                rider.AddConsumer<StockReservedConsumer>();
                rider.AddConsumer<StockReservationFailedConsumer>();
                rider.AddConsumer<PriceCalculatedConsumer>();

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
                });

                rider.AddProducer<OrderCreatedEvent>("orders.created");
                rider.AddProducer<OrderCancelledEvent>("orders.cancelled");
            });

            x.AddMessageScheduler(new Uri("queue:message-scheduler-queue"));

            x.AddConsumer<OrderSagaTimeoutConsumer>();

            x.UsingInMemory((context, cfg) =>
            {
                cfg.ReceiveEndpoint("saga-timeout-queue", e =>
                {
                    e.ConfigureConsumer<OrderSagaTimeoutConsumer>(context);
                });
            });
        });

        services.AddScoped<IPublishEndpoint>(sp => sp.GetRequiredService<IBus>());
    }
}

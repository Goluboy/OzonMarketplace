using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProductService.Application.EventHandlers;
using ProductService.Infrastructure.Abstractions.Caching.Abstractions;
using ProductService.Infrastructure.Abstractions.DTO.Product.Query;
using ProductService.Infrastructure.Abstractions.EventHandlers.Abstractions;
using ProductService.Infrastructure.Abstractions.EventPublisher.Abstractions;
using ProductService.Infrastructure.Abstractions.Repository.Abstractions;
using ProductService.Infrastructure.Abstractions.Repository.Abstractions.Products;
using ProductService.Infrastructure.Abstractions.UnitOfWork.Abstractions;
using ProductService.Infrastructure.Caching;
using ProductService.Infrastructure.Helpers.JsonbSerialization;
using ProductService.Infrastructure.Persistence.Provider;
using ProductService.Infrastructure.Repository;
using ProductService.Infrastructure.Repository.Decorators;
using ProductService.Infrastructure.Repository.Products;
using ProductService.Infrastructure.Saga.Dispatchers;
using ProductService.Infrastructure.Saga.EventPublisher;
using ProductService.Infrastructure.UnitOfWork;
using Redis.Service;

namespace ProductService.Infrastructure;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        DefaultTypeMap.MatchNamesWithUnderscores = true;

        SqlMapper.AddTypeHandler(new JsonbTypeHandler<List<ProductImageDto>>());

        services.AddSingleton<IPostgresConnectionFactory, PostgresConnectionFactory>();

        services.AddScoped<UnitOfWork.UnitOfWork>();

        services.AddScoped<IDbSession>(sp => sp.GetRequiredService<UnitOfWork.UnitOfWork>());
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<UnitOfWork.UnitOfWork>());

        services.AddSingleton<RedisCategoryVersionProvider>();
        services.AddSingleton<ICategoryVersionProvider>(sp => sp.GetRequiredService<RedisCategoryVersionProvider>());
        services.AddSingleton<ICategoryVersionUpdater>(sp => sp.GetRequiredService<RedisCategoryVersionProvider>());

        // Декоратор над CategoryRepository
        services.AddScoped<CategoryRepository>();
        services.AddScoped<ICategoryRepository>(sp => new CachedCategoryRepository(
            sp.GetRequiredService<CategoryRepository>(),
            sp.GetRequiredService<ICacheService>(),
            sp.GetRequiredService<IUnitOfWork>(),
            sp.GetRequiredService<ILogger<CachedCategoryRepository>>()));

        // Декоратор над ProductQueryService
        services.AddScoped<ProductQueryRepository>();
        services.AddScoped<IProductQueryRepository>(sp => new CachedProductQueryRepository(
            sp.GetRequiredService<ProductQueryRepository>(),
            sp.GetRequiredService<ICacheService>()));

        // Декоратор над ProductRepository
        services.AddScoped<ProductRepository>();
        services.AddScoped<IProductRepository>(sp => new CachedProductRepository(
            sp.GetRequiredService<ProductRepository>(),
            sp.GetRequiredService<ICacheService>(),
            sp.GetRequiredService<IUnitOfWork>(),
            sp.GetRequiredService<ILogger<CachedProductRepository>>()));

        services.AddScoped<IEventPublisher, EventPublisher>();
        services.AddScoped<IOrderCreatedEventHandler, OrderCreatedEventHandler>();
        services.AddScoped<IStockReservedEventHandler, StockReservedEventHandler>();
        services.AddScoped<OrderEventDispatcher>();
        services.AddScoped<ProductEventDispatcher>();

        services.AddCap(x =>
        {
            x.UsePostgreSql(opt =>
            {
                opt.ConnectionString = configuration.GetConnectionString("PostgresConnectionString")
                                       ?? throw new NullReferenceException(
                                           "Connection string 'PostgresConnectionString' not found.");
                opt.Schema = "cap";
            });

            x.UseKafka(kafka =>
            {
                kafka.Servers = configuration.GetValue<string>("KafkaSettings:Servers")
                                ?? throw new NullReferenceException("Connection string 'KafkaSettings' not found.");
            });

            x.FailedRetryCount = 5;
            x.FailedRetryInterval = 60;

            x.SucceedMessageExpiredAfter = 24 * 3600;
            
            x.UseDashboard(opt =>
            {
                opt.PathMatch = "/cap";
            });

            x.DefaultGroupName = "product-service-group";
            x.GroupNamePrefix = "product-service";
        });
        
        return services;
    }
}
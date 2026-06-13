using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProductService.Infrastructure.Abstractions.Caching.Abstractions;
using ProductService.Infrastructure.Abstractions.DTO.Product.Query;
using ProductService.Infrastructure.Abstractions.Repository.Abstractions;
using ProductService.Infrastructure.Abstractions.Repository.Abstractions.Products;
using ProductService.Infrastructure.Abstractions.UnitOfWork.Abstractions;
using ProductService.Infrastructure.Caching;
using ProductService.Infrastructure.Helpers.JsonbSerialization;
using ProductService.Infrastructure.Persistence.Provider;
using ProductService.Infrastructure.Repository;
using ProductService.Infrastructure.Repository.Decorators;
using ProductService.Infrastructure.Repository.Products;
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
            sp.GetRequiredService<ICacheService>()));
        
        // Декоратор над ProductQueryService
        services.AddScoped<ProductQueryRepository>();
        services.AddScoped<IProductQueryRepository>(sp => new CachedProductQueryRepository(
            sp.GetRequiredService<ProductQueryRepository>(),
            sp.GetRequiredService<ICacheService>()));
        
        // Декоратор над ProductRepository
        services.AddScoped<ProductRepository>();
        services.AddScoped<IProductRepository>(sp => new CachedProductRepository(
            sp.GetRequiredService<ProductRepository>(),
            sp.GetRequiredService<ICacheService>()));
        
        services.AddCap(x =>
        {
            x.UsePostgreSql(opt =>
            {
                opt.ConnectionString = configuration.GetConnectionString("PostgresConnectionString") 
                                       ?? throw new NullReferenceException("Connection string 'PostgresConnectionString' not found.");
                opt.Schema = "cap";
            });
            
            x.UseKafka(kafka =>
            {
                kafka.Servers = configuration.GetValue<string>("KafkaSettings:Servers") 
                                ?? throw new NullReferenceException("Connection string 'KafkaSettings' not found."); 
            });

            x.UseDashboard();
        });
        
        return services;
    }
}
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProductService.Infrastructure.Abstractions.DTO.Product.Query;
using ProductService.Infrastructure.Abstractions.Repository.Abstractions;
using ProductService.Infrastructure.Abstractions.Repository.Abstractions.Products;
using ProductService.Infrastructure.Abstractions.UnitOfWork.Abstractions;
using ProductService.Infrastructure.DAO;
using ProductService.Infrastructure.Helpers.JsonbSerialization;
using ProductService.Infrastructure.Persistence.Provider;
using ProductService.Infrastructure.Repository;
using ProductService.Infrastructure.Repository.Products;
using ProductService.Infrastructure.UnitOfWork;

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
        
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IProductQueryRepository, ProductQueryRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        
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
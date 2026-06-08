using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        SqlMapper.AddTypeHandler(new JsonbTypeHandler<List<ProductImageDao>>());

        services.AddSingleton<IPostgresConnectionFactory, PostgresConnectionFactory>();

        services.AddScoped<UnitOfWork.UnitOfWork>();
        
        services.AddScoped<IDbSession>(sp => sp.GetRequiredService<UnitOfWork.UnitOfWork>());
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<UnitOfWork.UnitOfWork>());
        
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IProductQueryRepository, ProductQueryRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        
        return services;
    }
}
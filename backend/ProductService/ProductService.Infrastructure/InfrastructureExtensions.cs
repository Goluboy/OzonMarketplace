using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProductService.Infrastructure.DAO;
using ProductService.Infrastructure.Helpers.JsonbSerialization;

namespace ProductService.Infrastructure;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        SqlMapper.AddTypeHandler(new JsonbTypeHandler<List<ProductImageDao>>());
        
        return services;
    }
}
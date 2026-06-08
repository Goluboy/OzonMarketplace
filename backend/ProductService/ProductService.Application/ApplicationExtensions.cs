using Microsoft.Extensions.DependencyInjection;
using ProductService.Application.Services.Categories;
using ProductService.Application.Services.Products;
using ProductService.Application.Services.Products.Command;
using ProductService.Application.Services.Products.Query;
using ProductService.Infrastructure.Abstractions.Repository.Abstractions.Products;

namespace ProductService.Application;

public static class ApplicationExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IProductQueryService, ProductQueryService>();
        services.AddScoped<IProductCommandService, ProductCommandService>();
        
        return services;
    }
}
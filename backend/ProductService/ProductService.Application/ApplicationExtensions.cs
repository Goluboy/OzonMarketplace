using Microsoft.Extensions.DependencyInjection;
using ProductService.Application.Services.Categories;
using ProductService.Application.Services.Media;
using ProductService.Application.Services.Products;
using ProductService.Application.Services.Products.Command;
using ProductService.Application.Services.Products.Query;
using ProductService.Infrastructure.Abstractions.Repository.Abstractions.Products;

namespace ProductService.Application;

public static class ApplicationExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IProductQueryService, ProductQueryService>();
        services.AddScoped<IProductCommandService, ProductCommandService>();
        services.AddScoped<IMediaService, MediaService>();
        
        return services;
    }
}
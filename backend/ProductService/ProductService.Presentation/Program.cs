using Core.Minio;
using ProductService.Application;
using ProductService.Infrastructure;
using ProductService.Infrastructure.Persistence;
using Serilog;

namespace ProductService.Presentation;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        builder.Host.UseSerilog((context, configuration) => 
            configuration.ReadFrom.Configuration(context.Configuration));
            
        var services = builder.Services;
        
        services.AddControllers();

        ConfigureServices(services, builder.Configuration);
        
        var app = builder.Build();
        
        app.Configuration.RunMigrations();
            
        app.ConfigureMiddleware();
            
        app.MapControllers();
            
        app.Run();
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddInfrastructure(configuration)
            .AddApplication()
            .AddPresentation()
            .AddMinioStorage(options =>
            {
                configuration.GetSection("Minio").Bind(options);
            });
    }
    
    private static void ConfigureMiddleware(this IApplicationBuilder builder)
    {
        builder.UseExceptionHandler();
        
        builder.UseSwagger();
        builder.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Marketplace API v1");
            options.RoutePrefix = "swagger";
        });
    }
}
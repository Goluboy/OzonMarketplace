
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

        services.AddInfrastructure(builder.Configuration)
            .AddApplication()
            .AddPresentation();
        
        var app = builder.Build();
        
        app.Configuration.RunMigrations();
            
        app.ConfigureMiddleware();
            
        app.MapControllers();
            
        app.Run();
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
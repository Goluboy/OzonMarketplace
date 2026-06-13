using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.OpenApi.Models;
using ProductService.Presentation.Middleware;
using ProductService.Presentation.Models;

namespace ProductService.Presentation;

public static class PresentationExtension
{
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        services.AddProblemDetails();
        services.AddExceptionHandler<ExceptionHandler>();
        
        services.AddFluentValidationAutoValidation();
        services.AddValidatorsFromAssemblyContaining<UpsertCategoryRequest>();

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo 
            { 
                Title = "Marketplace API", 
                Version = "v1",
                Description = "Product Service API с интеграцией Keycloak JWT"
            });
            
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Введите JWT-токен доступа (Access Token), полученный от Keycloak.\n\nШаблон: {токен}"
            });
            
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }
}
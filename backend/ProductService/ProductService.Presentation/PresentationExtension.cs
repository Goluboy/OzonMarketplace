using FluentValidation;
using FluentValidation.AspNetCore;
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
        services.AddSwaggerGen();
        
        return services;
    }
}
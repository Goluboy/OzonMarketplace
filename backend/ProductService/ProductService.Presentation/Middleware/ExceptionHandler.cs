using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using ProductService.Application.Exceptions;

namespace ProductService.Presentation.Middleware;

public class ExceptionHandler(ILogger<ExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);
        
        var (statusCode, title, detail, errors) = exception switch
        {
            ValidationException valEx => (
                StatusCodes.Status400BadRequest,
                "One or more validation errors occurred.",
                "Validation failed.",
                MapValidationErrors(valEx)
            ),
            
            PostgresException { SqlState: "23505", ConstraintName: "uq_products_seller_sku" } => (
                StatusCodes.Status409Conflict,
                "Product SKU Conflict",
                "A product with this SKU already exists in your catalog. SKU must be unique within your seller account.",
                null
            ),
            
            ForbiddenException => (
                StatusCodes.Status403Forbidden,
                "Access to this resource is forbidden.",
                exception.Message,
                null
            ),
            
            KeyNotFoundException or NotFoundException => (
                StatusCodes.Status404NotFound,
                "The specified resource was not found.",
                exception.Message,
                null
            ),
            
            _ => (
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred.",
                "An unexpected error occurred on the server.",
                null
            )
        };
        
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Type = $"https://httpstatuses.com/{statusCode}",
            Detail = detail,
            Instance = httpContext.Request.Path
        };
        
        if (errors != null)
        {
            problemDetails.Extensions["errors"] = errors;
        }
        
        httpContext.Response.StatusCode = statusCode;
        
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
    
    private static Dictionary<string, string[]> MapValidationErrors(ValidationException exception)
    {
        return exception.Errors
            .GroupBy(x => x.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.ErrorMessage).ToArray()
            );
    }
}
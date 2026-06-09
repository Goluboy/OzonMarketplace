using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ProductService.Application.Exceptions;

namespace ProductService.Presentation.Middleware;

public class ExceptionHandler(ILogger<ExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);
        
        var (statusCode, title, errors) = exception switch
        {
            ValidationException valEx => (
                StatusCodes.Status400BadRequest,
                "One or more validation errors occurred.",
                MapValidationErrors(valEx)
            ),
            KeyNotFoundException or NotFoundException => (
                StatusCodes.Status404NotFound,
                "The specified resource was not found.",
                null
            ),
            _ => (
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred.",
                null
            )
        };
        
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Type = $"https://httpstatuses.com/{statusCode}",
            Detail = exception.Message,
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
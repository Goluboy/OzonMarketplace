using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace OrderService.Http.Middleware;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> logger;
    private readonly IHostEnvironment environment;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger,
        IHostEnvironment environment)
    {
        this.logger = logger;
        this.environment = environment;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var traceId = httpContext.TraceIdentifier;

        // Определяем уровень логирования по типу исключения
        var logLevel = exception switch
        {
            ValidationException => LogLevel.Warning,
            ArgumentException => LogLevel.Warning,
            InvalidOperationException => LogLevel.Warning,
            KeyNotFoundException => LogLevel.Warning,
            UnauthorizedAccessException => LogLevel.Warning,
            PostgresException { SqlState: "23505" } => LogLevel.Warning,
            _ => LogLevel.Error
        };

        logger.Log(logLevel, exception,
            "Exception occurred | TraceId: {TraceId} | Method: {Method} | Path: {Path} | User: {User} | Type: {ExceptionType}",
            traceId,
            httpContext.Request.Method,
            httpContext.Request.Path,
            httpContext.User.Identity?.Name ?? "anonymous",
            exception.GetType().Name);

        // Маппим исключение на (statusCode, title, detail)
        var (statusCode, title, detail) = MapException(exception);

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Type = $"https://httpstatuses.com/{statusCode}",
            Instance = httpContext.Request.Path,
            Extensions =
            {
                ["traceId"] = traceId,
                ["timestamp"] = DateTime.UtcNow
            }
        };

        // В Development добавляем техническую информацию для отладки
        if (environment.IsDevelopment())
        {
            problemDetails.Extensions["exceptionType"] = exception.GetType().FullName;
            problemDetails.Extensions["stackTrace"] = exception.StackTrace;

            if (exception.InnerException != null)
            {
                problemDetails.Extensions["innerException"] = new
                {
                    type = exception.InnerException.GetType().FullName,
                    message = exception.InnerException.Message
                };
            }
        }

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    /// <summary>
    /// Маппинг исключений на HTTP status codes
    /// </summary>
    private (int statusCode, string title, string detail) MapException(Exception exception)
    {
        return exception switch
        {
            ValidationException validationException => (
                StatusCodes.Status400BadRequest,
                "Validation failed",
                FormatValidationErrors(validationException)
            ),

            ArgumentException or ArgumentNullException => (
                StatusCodes.Status400BadRequest,
                "Invalid argument",
                exception.Message
            ),

            KeyNotFoundException or FileNotFoundException => (
                StatusCodes.Status404NotFound,
                "Resource not found",
                exception.Message
            ),

            InvalidOperationException => (
                StatusCodes.Status409Conflict,
                "Operation cannot be performed",
                exception.Message
            ),

            UnauthorizedAccessException => (
                StatusCodes.Status403Forbidden,
                "Access denied",
                exception.Message
            ),

            NotSupportedException => (
                StatusCodes.Status501NotImplemented,
                "Operation not supported",
                exception.Message
            ),

            PostgresException postgresException => MapPostgresException(postgresException),

            TimeoutException or OperationCanceledException => (
                StatusCodes.Status504GatewayTimeout,
                "Operation timeout",
                "The operation took too long to complete. Please try again later."
            ),

            HttpRequestException => (
                StatusCodes.Status502BadGateway,
                "External service error",
                environment.IsDevelopment()
                    ? exception.Message
                    : "An external service is currently unavailable."
            ),

            _ => (
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred",
                environment.IsDevelopment()
                    ? exception.Message
                    : "An unexpected error occurred. Please try again later."
            )
        };
    }

    private (int statusCode, string title, string detail) MapPostgresException(PostgresException ex)
    {
        return ex.SqlState switch
        {
            "23505" => (
                StatusCodes.Status409Conflict,
                "Duplicate record",
                $"A record with this data already exists. Constraint: {ex.ConstraintName}"
            ),

            "23503" => (
                StatusCodes.Status409Conflict,
                "Referenced record not found",
                "The referenced record does not exist."
            ),

            "23514" => (
                StatusCodes.Status400BadRequest,
                "Data validation failed",
                "The provided data does not meet the required constraints."
            ),

            "23502" => (
                StatusCodes.Status400BadRequest,
                "Required field missing",
                $"Field '{ex.ColumnName}' is required."
            ),

            "40P01" or "40001" => (
                StatusCodes.Status409Conflict,
                "Concurrent modification",
                "The record was modified by another operation. Please retry."
            ),

            _ => (
                StatusCodes.Status500InternalServerError,
                "Database error",
                environment.IsDevelopment()
                    ? $"Database error: {ex.Message}"
                    : "A database error occurred."
            )
        };
    }

    private static string FormatValidationErrors(ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(e => e.PropertyName)
            .Select(g => $"{g.Key}: {string.Join(", ", g.Select(e => e.ErrorMessage))}");

        return string.Join("; ", errors);
    }
}
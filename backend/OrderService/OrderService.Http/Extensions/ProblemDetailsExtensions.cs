using Microsoft.AspNetCore.Mvc;

namespace OrderService.Http.Extensions;

public static class ProblemDetailsExtensions
{
    public static ProblemDetails CreateProblemDetails(
        this HttpContext httpContext,
        string title,
        string? detail = null,
        int statusCode = StatusCodes.Status400BadRequest)
    {
        return new ProblemDetails
        {
            Type = $"https://httpstatuses.com/{statusCode}",
            Title = title,
            Detail = detail,
            Status = statusCode,
            Instance = httpContext.Request.Path
        };
    }
}

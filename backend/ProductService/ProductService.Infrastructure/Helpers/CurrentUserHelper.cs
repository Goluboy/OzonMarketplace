using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using ProductService.Infrastructure.Abstractions.Helpers.Abstractions;

namespace ProductService.Infrastructure.Helpers;

public class CurrentUserHelper(IHttpContextAccessor httpContextAccessor) : ICurrentUserHelper
{
    public Guid UserId
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;
            var userIdStr = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return Guid.TryParse(userIdStr, out var userId) 
                ? userId 
                : throw new UnauthorizedAccessException("You are not authorized to access this resource.");
        }
    }
    
    public bool IsAdmin => httpContextAccessor.HttpContext?.User?.IsInRole("admin") ?? false;
}
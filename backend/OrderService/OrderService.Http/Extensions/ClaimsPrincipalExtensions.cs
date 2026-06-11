using System.Security.Claims;

namespace OrderService.Http.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? user.FindFirstValue("sub");

        if (id is null || !Guid.TryParse(id, out var userId))
        {
            throw new UnauthorizedAccessException("User identifier claim is missing or invalid.");
        }

        return userId;
    }

    public static bool IsAdmin(this ClaimsPrincipal user) =>
        user.IsInRole("admin");
}

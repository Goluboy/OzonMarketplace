using System.Security.Claims;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier)
              ?? user.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(id) || !Guid.TryParse(id, out var userId))
        {
            throw new UnauthorizedAccessException(
                "User identifier claim ('sub') is missing or invalid.");
        }

        return userId;
    }

    public static string GetEmail(this ClaimsPrincipal user)
    {
        var email = user.FindFirstValue(ClaimTypes.Email)
                 ?? user.FindFirstValue("email");

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new UnauthorizedAccessException(
                "Email claim is missing in token.");
        }

        return email;
    }

    public static string GetName(this ClaimsPrincipal user)
    {
        var fullName = user.FindFirstValue(ClaimTypes.Name)
                    ?? user.FindFirstValue("name");

        if (!string.IsNullOrWhiteSpace(fullName))
            return fullName;

        var givenName = user.FindFirstValue(ClaimTypes.GivenName)
                     ?? user.FindFirstValue("given_name");
        var familyName = user.FindFirstValue(ClaimTypes.Surname)
                      ?? user.FindFirstValue("family_name");

        if (!string.IsNullOrWhiteSpace(givenName) || !string.IsNullOrWhiteSpace(familyName))
            return $"{givenName} {familyName}".Trim();

        var preferredUsername = user.FindFirstValue("preferred_username");
        if (!string.IsNullOrWhiteSpace(preferredUsername))
            return preferredUsername;

        throw new UnauthorizedAccessException(
            "Name claim is missing in token.");
    }

    public static string? GetGivenName(this ClaimsPrincipal user)
    {
        return user.FindFirstValue(ClaimTypes.GivenName)
            ?? user.FindFirstValue("given_name");
    }

    public static string? GetFamilyName(this ClaimsPrincipal user)
    {
        return user.FindFirstValue(ClaimTypes.Surname)
            ?? user.FindFirstValue("family_name");
    }

    public static string? GetPreferredUsername(this ClaimsPrincipal user)
    {
        return user.FindFirstValue("preferred_username");
    }

    public static bool IsEmailVerified(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue("email_verified");
        return bool.TryParse(value, out var verified) && verified;
    }

    public static IReadOnlyList<string> GetRoles(this ClaimsPrincipal user)
    {
        return user.FindAll("roles")
            .Select(c => c.Value)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Where(v => !IsSystemRole(v))
            .Distinct()
            .ToList();
    }

    public static bool IsCustomer(this ClaimsPrincipal user) =>
        user.IsInRole("customer");

    public static bool IsAdmin(this ClaimsPrincipal user) =>
        user.IsInRole("admin");

    public static bool IsSeller(this ClaimsPrincipal user) =>
        user.IsInRole("seller");

    public static bool HasRole(this ClaimsPrincipal user, string role) =>
        user.IsInRole(role);

    public static bool HasAnyRole(this ClaimsPrincipal user, params string[] roles) =>
        roles.Any(role => user.IsInRole(role));

    private static bool IsSystemRole(string role)
    {
        return role.StartsWith("default-roles-", StringComparison.OrdinalIgnoreCase)
            || role == "offline_access"
            || role == "uma_authorization";
    }
    public static UserProfile GetProfile(this ClaimsPrincipal user)
    {
        return new UserProfile
        {
            Id = user.GetUserId(),
            Email = user.GetEmail(),
            Name = user.GetName(),
            GivenName = user.GetGivenName(),
            FamilyName = user.GetFamilyName(),
            PreferredUsername = user.GetPreferredUsername(),
            EmailVerified = user.IsEmailVerified(),
            Roles = user.GetRoles()
        };
    }
}

public class UserProfile
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? GivenName { get; set; }
    public string? FamilyName { get; set; }
    public string? PreferredUsername { get; set; }
    public bool EmailVerified { get; set; }
    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
}
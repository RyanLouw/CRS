using System.Security.Claims;
using CRS.Core.Helpers;

namespace CRS.Services;

public sealed class UserSession(IHttpContextAccessor httpContextAccessor) : IUserSession
{
    private ClaimsPrincipal? GetCurrentUser()
    {
        var user = httpContextAccessor.HttpContext?.User;
        return user?.Identity?.IsAuthenticated == true ? user : null;
    }

    public Guid? GetCurrentUserId() => GetCurrentUser() is { } user
        ? ClaimsHelpers.GetUserId(user) : null;

    public string GetUserName()
    {
        var user = GetCurrentUser();
        if (user is null) return "Unknown";

        var displayName = FirstValue(user, "name", ClaimTypes.Name);
        if (displayName is not null) return displayName;

        var fullName = string.Join(" ", new[]
        {
            FirstValue(user, "given_name", ClaimTypes.GivenName),
            FirstValue(user, "family_name", ClaimTypes.Surname)
        }.Where(value => !string.IsNullOrWhiteSpace(value)));
        return fullName.Length > 0 ? fullName : user.Identity?.Name ?? "Unknown";
    }

    public string? GetCurrentUserEmail() => GetCurrentUser() is { } user
        ? FirstValue(user, ClaimTypes.Email, "email", "preferred_username", "upn", ClaimTypes.Upn)
            ?? user.Identity?.Name
        : null;

    private static string? FirstValue(ClaimsPrincipal user, params string[] types) =>
        types.Select(type => user.FindFirstValue(type)).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
}

using System.Security.Claims;

namespace CRS.Core.Helpers;

public static class ClaimsHelpers
{
    public static Guid? GetUserId(ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true) return null;

        // Entra's object ID is the directory user key, not an authentication token.
        var value = user.FindFirst("oid")?.Value
            ?? user.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(value, out var id) && id != Guid.Empty ? id : null;
    }
}

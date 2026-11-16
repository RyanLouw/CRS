using System.Security.Claims;

namespace CRS.Core.Helpers;

public static class ClaimsHelpers
{
    // Retain the legacy PSP/IT/Admin role-name convention for call recordings.
    public static bool CanDownloadRecordings(ClaimsPrincipal user) => user.Identity?.IsAuthenticated == true &&
        user.Claims.Where(claim => claim.Type is "roles" or ClaimTypes.Role).Any(claim =>
            claim.Value.Contains("PSP", StringComparison.Ordinal) || claim.Value.Contains("IT", StringComparison.Ordinal) ||
            claim.Value.Contains("Admin", StringComparison.Ordinal));

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

using System.Security.Claims;
using CRS.Core.Models;

namespace CRS.Security;

public static class UserContext
{
    public static Guid UserId(ClaimsPrincipal user)
    {
        var value = user.FindFirstValue("oid") ?? user.FindFirstValue("http://schemas.microsoft.com/identity/claims/objectidentifier")
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) && id != Guid.Empty ? id
            : throw new WorkflowException("Your sign-in does not contain a valid directory user ID. Contact IT.");
    }
}

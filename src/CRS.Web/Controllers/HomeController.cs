using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CRS.Controllers;

public sealed class HomeController : Controller
{
    [AllowAnonymous]
    [IgnoreAntiforgeryToken] // Read-only error page can also render a forbidden POST.
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult AccessDenied(string? returnUrl = null)
    {
        var original = HttpContext.Features.Get<IStatusCodeReExecuteFeature>();
        var requestedPage = original is not null
            ? original.OriginalPathBase + original.OriginalPath
            : returnUrl;
        ViewBag.ReturnUrl = Url.IsLocalUrl(requestedPage) ? requestedPage : null;
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View();
    }
}

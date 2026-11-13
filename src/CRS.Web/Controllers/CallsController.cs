using CRS.Core.Models;
using CRS.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRS.Controllers;

[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class CallsController(ICallManager manager, TimeProvider clock) : Controller
{
    public async Task<IActionResult> Index(DateOnly? date, CancellationToken ct)
    {
        if (!ModelState.IsValid) throw new WorkflowException("Choose a valid call date.");
        return View(await manager.GetCallsAsync(UserContext.UserId(User), date ?? DateOnly.FromDateTime(clock.GetLocalNow().DateTime), ct));
    }

    public async Task<IActionResult> Rate(string callId, DateOnly date, CancellationToken ct) =>
        View(await manager.GetRatingPageAsync(UserContext.UserId(User), callId, date, ct));

    [HttpPost]
    public async Task<IActionResult> Rate(RatingInput input, CancellationToken ct)
    {
        try
        {
            if (ModelState.IsValid)
            {
                // Use server-observed addresses; do not trust arbitrary X-Forwarded-For headers.
                var result = await manager.SaveAsync(UserContext.UserId(User), input,
                    HttpContext.Connection.RemoteIpAddress?.ToString(), HttpContext.Connection.LocalIpAddress?.ToString(), ct);
                TempData[result.Saved && result.NotificationSent ? "Success" : "Warning"] = result.Message;
                return RedirectToAction(nameof(Index), new { date = input.Date.ToString("yyyy-MM-dd") });
            }
        }
        catch (WorkflowException ex) { ModelState.AddModelError("", ex.Message); }
        var page = await manager.GetRatingPageAsync(UserContext.UserId(User), input.CallId, input.Date, ct);
        return View(page with { Input = input });
    }
    [Authorize(Policy = "Recordings")]
    public async Task<IActionResult> Recording(string callId, DateOnly date, CancellationToken ct) =>
        Redirect(await manager.GetRecordingAsync(UserContext.UserId(User), callId, date, ct));
}

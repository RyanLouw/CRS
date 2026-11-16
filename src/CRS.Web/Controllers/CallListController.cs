using CRS.Services;
using Microsoft.AspNetCore.Authorization;
using CRS.Core.Models;
using CRS.Core.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace CRS.Controllers;

[Authorize(Policy = "DirectoryUser")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class CallListController(ICallManager manager, TimeProvider clock, IUserSession userSession) : Controller
{
    private Guid CurrentUserId => userSession.GetCurrentUserId()
        ?? throw new UnauthorizedAccessException("An authenticated directory user is required.");

    public async Task<IActionResult> Index(DateOnly? date, CancellationToken ct)
    {
        if (!ModelState.IsValid) throw new WorkflowException("Choose a valid call date.");
        return View(await manager.GetCallsAsync(CurrentUserId, date ?? DateOnly.FromDateTime(clock.GetLocalNow().DateTime), ct));
    }

    private bool IsModalRequest => Request.Headers["X-Rating-Modal"] == "true";

    public async Task<IActionResult> Rate(string callId, DateOnly date, CancellationToken ct)
    {
        var page = await manager.GetRatingPageAsync(CurrentUserId, callId, date, ct);
        return IsModalRequest ? PartialView("_RatingForm", page) : View(page);
    }

    [HttpPost]
    public async Task<IActionResult> Rate(RatingInput input, CancellationToken ct)
    {
        try
        {
            if (ModelState.IsValid)
            {
                // Forwarded client IP is accepted only from the configured trusted proxies.
                var result = await manager.SaveAsync(CurrentUserId, input,
                    HttpContext.Connection.RemoteIpAddress?.ToString(), HttpContext.Items["OriginalRemoteIpAddress"] as string ?? HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
                TempData[result.Saved && result.NotificationSent ? "Success" : "Warning"] = result.Message;
                if (IsModalRequest) return Json(new { saved = result.Saved, notificationSent = result.NotificationSent });
                return RedirectToAction(nameof(Index), new { date = input.Date.ToString("yyyy-MM-dd") });
            }
        }
        catch (WorkflowException ex) { ModelState.AddModelError("", ex.Message); }
        var page = await manager.GetRatingPageAsync(CurrentUserId, input.CallId, input.Date, ct);
        if (IsModalRequest)
        {
            Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
            return PartialView("_RatingForm", page with { Input = input });
        }
        return View(page with { Input = input });
    }
    [Authorize(Policy = "CallRecording")]
    public async Task<IActionResult> Recording(string callId, DateOnly date, CancellationToken ct) =>
        Redirect(await manager.GetRecordingAsync(CurrentUserId, callId, date, ct));
}

using CRS.Services;
using Microsoft.AspNetCore.Authorization;
using CRS.Core.Models;
using CRS.Core.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace CRS.Controllers;

[Authorize(Policy = "CallRatingSystemAdmin")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminController(IAdminManager manager, IUserSession userSession) : Controller
{
    private Guid CurrentUserId => userSession.GetCurrentUserId()
        ?? throw new UnauthorizedAccessException("An authenticated directory user is required.");

    public async Task<IActionResult> Index(int page = 1, long? statusId = null, CancellationToken ct = default)
    {
        if (!ModelState.IsValid) throw new WorkflowException("Choose a valid status and page number.");
        return View(await manager.GetPageAsync(page, statusId, ct));
    }
    private bool IsModalRequest => Request.Headers["X-Rating-Modal"] == "true";
    public async Task<IActionResult> Edit(long id, CancellationToken ct)
    {
        var page = await manager.GetEditAsync(id, ct);
        return IsModalRequest ? PartialView("_EditForm", page) : View(page);
    }
    [HttpPost]
    public async Task<IActionResult> Edit(UpdateInput input, CancellationToken ct)
    {
        try
        {
            if (ModelState.IsValid)
            {
                var result = await manager.UpdateAsync(CurrentUserId, input, ct);
                TempData[result.NotificationSent ? "Success" : "Warning"] = result.Message;
                if (IsModalRequest) return Json(new { saved = result.Saved, notificationSent = result.NotificationSent });
                return RedirectToAction(nameof(Index));
            }
        }
        catch (WorkflowException ex) { ModelState.AddModelError("", ex.Message); }
        var page = await manager.GetEditAsync(input.Id, ct);
        if (IsModalRequest)
        {
            Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
            return PartialView("_EditForm", page with { Input = input });
        }
        return View(page with { Input = input });
    }
    public async Task<IActionResult> Recording(long id, CancellationToken ct) => Redirect(await manager.GetRecordingAsync(id, ct));
    public async Task<IActionResult> Export(long? statusId, CancellationToken ct)
    {
        if (!ModelState.IsValid) throw new WorkflowException("Choose a valid status filter.");
        return File(await manager.ExportAsync(statusId, ct), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "CallRatings.xlsx");
    }
}

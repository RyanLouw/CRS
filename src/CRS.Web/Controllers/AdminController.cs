using CRS.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace CRS.Controllers;

[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminController(IAdminManager manager, IConfiguration configuration) : Controller
{
    // Authentication is intentionally left for the owner. Replace this configuration lookup
    // with the signed-in directory user ID when wiring authentication.
    private Guid CurrentUserId => Guid.TryParse(configuration["CurrentUser:Id"], out var id) && id != Guid.Empty
        ? id : throw new WorkflowException("Configure CurrentUser:Id until sign-in is connected.");

    public async Task<IActionResult> Index(int page = 1, long? statusId = null, CancellationToken ct = default)
    {
        if (!ModelState.IsValid) throw new WorkflowException("Choose a valid status and page number.");
        return View(await manager.GetPageAsync(page, statusId, ct));
    }
    public async Task<IActionResult> Edit(long id, CancellationToken ct) => View(await manager.GetEditAsync(id, ct));
    [HttpPost]
    public async Task<IActionResult> Edit(UpdateInput input, CancellationToken ct)
    {
        try
        {
            if (ModelState.IsValid)
            {
                var result = await manager.UpdateAsync(CurrentUserId, input, ct);
                TempData[result.NotificationSent ? "Success" : "Warning"] = result.Message;
                return RedirectToAction(nameof(Index));
            }
        }
        catch (WorkflowException ex) { ModelState.AddModelError("", ex.Message); }
        var page = await manager.GetEditAsync(input.Id, ct);
        return View(page with { Input = input });
    }
    public async Task<IActionResult> Recording(long id, CancellationToken ct) => Redirect(await manager.GetRecordingAsync(id, ct));
    public async Task<IActionResult> Export(long? statusId, CancellationToken ct)
    {
        if (!ModelState.IsValid) throw new WorkflowException("Choose a valid status filter.");
        return File(await manager.ExportAsync(statusId, ct), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "CallRatings.xlsx");
    }
}

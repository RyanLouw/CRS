using ClosedXML.Excel;
using CRS.Core.Models;
using CRS.Core.Models.DTO;
using CRS.Core.Models.ViewModels;
using Serilog;
using CRS.Core.DataAccess;

namespace CRS.Core.Managers;

public sealed class AdminManager(IRatingRepository repository, CallApiDataAccess api, IRatingNotifier notifier,
    TimeProvider clock) : IAdminManager
{
    public async Task<AdminPage> GetPageAsync(int page, long? statusId, CancellationToken ct)
    {
        var lookups = await repository.GetLookupsAsync(ct);
        ValidateStatus(statusId, lookups);
        var rows = await repository.GetRatingsAsync(statusId, ct);
        // The legacy ID-subtraction paging skips rows when IDs have gaps or a filter is used.
        // Page the actual filtered, ordered result instead.
        var count = Math.Max(1, (int)Math.Ceiling(rows.Count / 5d));
        page = Math.Clamp(page, 1, count);
        return new(rows.OrderByDescending(r => r.CaptureDate).ThenByDescending(r => r.Id)
            .Skip((page - 1) * 5).Take(5).ToArray(), lookups, page, count, statusId);
    }
    private static void ValidateStatus(long? status, Lookups lookups)
    {
        if (status.HasValue && !lookups.Statuses.Any(s => s.Id == status.Value))
            throw new WorkflowException("Select a valid status filter.");
    }
    public async Task<AdminEditPage> GetEditAsync(long id, CancellationToken ct)
    {
        var rating = await repository.FindAsync(id, ct) ?? throw new MissingRecordException("The rating was not found.");
        return new(rating, await repository.GetLookupsAsync(ct), new()
            { Id = id, StatusId = rating.StatusId, ReasonId = rating.ReasonId ?? 0, Note = rating.ITNote });
    }
    public async Task<SaveResult> UpdateAsync(Guid userId, UpdateInput input, CancellationToken ct)
    {
        if (userId == Guid.Empty) throw new WorkflowException("A valid signed-in user is required.");
        var page = await GetEditAsync(input.Id, ct);
        var status = page.Lookups.Statuses.FirstOrDefault(s => s.Id == input.StatusId)
            ?? throw new WorkflowException("Select a valid help-desk status.");
        var reason = page.Lookups.Reasons.FirstOrDefault(s => s.Id == input.ReasonId)
            ?? throw new WorkflowException("Select a valid help-desk reason.");
        if (input.Note?.Length > 4000) throw new WorkflowException("IT notes must be 4000 characters or fewer.");
        var now = clock.GetLocalNow().DateTime;
        if (!await repository.UpdateAsync(input.Id, userId, now, input, ct))
            throw new MissingRecordException("The rating no longer exists.");
        Log.Information("Help-desk rating updated; rating {RatingId}", input.Id);
        var updated = page.Rating with { StatusId = status.Id, Status = status.Name, ReasonId = reason.Id,
            Reason = reason.Name, ITNote = input.Note?.Trim(), UpdatedAt = now, UpdatedBy = userId };
        try
        {
            await notifier.NotifyAsync(updated, true, ct);
            return new(true, true, "Help-desk update saved and notifications sent.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            Log.Warning("Help-desk update saved but notification failed with {ErrorType}", ex.GetType().Name);
            return new(true, false, "Update saved, but an email notification failed. Contact IT.");
        }
    }
    public async Task<string> GetRecordingAsync(long id, CancellationToken ct)
    {
        var rating = await repository.FindAsync(id, ct) ?? throw new MissingRecordException("The rating was not found.");
        if (string.IsNullOrWhiteSpace(rating.RecordingKey)) throw new MissingRecordException("No recording is available.");
        var url = await api.GetRecordingAsync(rating.RecordingKey, ct)
            ?? throw new MissingRecordException("The recording is no longer available.");
        Log.Information("Help-desk recording lookup completed; rating {RatingId}", id);
        return url;
    }
    public async Task<byte[]> ExportAsync(long? statusId, CancellationToken ct)
    {
        ValidateStatus(statusId, await repository.GetLookupsAsync(ct));
        var rows = await repository.GetRatingsAsync(statusId, ct);
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Call ratings");
        string[] headers = ["Rating ID", "User", "Captured", "Extension", "Call time", "Number dialed", "Problems",
            "Other problem", "Pharmacy", "Status", "Reason", "IT note", "Updated at"];
        for (var col = 0; col < headers.Length; col++) sheet.Cell(1, col + 1).Value = headers[col];
        var row = 2;
        foreach (var rating in rows)
        {
            ct.ThrowIfCancellationRequested();
            object?[] values = [rating.Id, rating.DisplayName, rating.CaptureDate, rating.Extension, rating.CallDateTime,
                rating.NumberPhoned, rating.Problems, rating.OtherProblem, rating.Pharmacy, rating.Status,
                rating.Reason, rating.ITNote, rating.UpdatedAt];
            for (var col = 0; col < values.Length; col++)
                sheet.Cell(row, col + 1).Value = values[col] is DateTime d ? d.ToString("yyyy-MM-dd HH:mm:ss") : values[col]?.ToString() ?? "";
            row++;
        }
        sheet.Row(1).Style.Font.Bold = true;
        sheet.SheetView.FreezeRows(1);
        sheet.Columns().AdjustToContents(1, Math.Min(row, 100));
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        Log.Information("Call-rating report generated; row count {RowCount}", rows.Count);
        return stream.ToArray();
    }
}

using CRS.Core.Models;
using CRS.Core.Models.DTO;
using CRS.Core.Models.ViewModels;
using Serilog;
using CRS.Core.DataAccess;

namespace CRS.Core.Managers;

public sealed class CallManager(IRatingRepository repository, CallApiDataAccess api, IRatingNotifier notifier,
    TimeProvider clock) : ICallManager
{
    public async Task<CallListPage> GetCallsAsync(Guid userId, DateOnly date, CancellationToken ct)
    {
        if (userId == Guid.Empty) throw new WorkflowException("A valid signed-in user is required.");
        if (date == default || date > DateOnly.FromDateTime(clock.GetLocalNow().DateTime))
            throw new WorkflowException("Choose a valid call date that is not in the future.");
        var user = await repository.GetUserAsync(userId, ct)
            ?? throw new WorkflowException("Your account could not be found in the user directory. Contact IT.");
        if (string.IsNullOrWhiteSpace(user.Extension))
            throw new WorkflowException("Your account does not have a telephone extension. Contact IT.");
        var calls = await api.GetCallsAsync(user.Extension, date, ct);
        // Never trust upstream data for another extension or date.
        var owned = calls.Where(c => c.Extension == user.Extension && c.CallDateTime.HasValue &&
            DateOnly.FromDateTime(c.CallDateTime.Value) == date).ToArray();
        var lookups = await repository.GetLookupsAsync(ct);
        Log.Information("Call list loaded; count {CallCount}", owned.Length);
        return new(user, date, owned, lookups);
    }

    public async Task<RatingPage> GetRatingPageAsync(Guid userId, string callId, DateOnly date, CancellationToken ct)
    {
        var page = await GetCallsAsync(userId, date, ct);
        var call = FindCall(page, callId);
        return new(call, page.Lookups, new() { CallId = call.CallID, Date = date });
    }

    private static CallInfo FindCall(CallListPage page, string callId)
    {
        if (string.IsNullOrWhiteSpace(callId)) throw new WorkflowException("Select a call.");
        return page.Calls.FirstOrDefault(c => c.CallID == callId)
            ?? throw new MissingRecordException("The call was not found for your extension and selected date.");
    }

    public async Task<SaveResult> SaveAsync(Guid userId, RatingInput input, string? clientIP, string? hostIP, CancellationToken ct)
    {
        var page = await GetCallsAsync(userId, input.Date, ct);
        var call = FindCall(page, input.CallId);
        var ids = (input.ProblemIds ?? []).Distinct().ToArray();
        var problems = page.Lookups.Problems.Where(p => ids.Contains(p.Id)).ToArray();
        if (ids.Length == 0 || problems.Length != ids.Length)
            throw new WorkflowException("Select valid problems from the list.");
        if (input.OtherProblem?.Length > 4000) throw new WorkflowException("Other details must be 4000 characters or fewer.");
        if (problems.Any(p => p.Name.Trim().Equals("Other", StringComparison.OrdinalIgnoreCase)) && string.IsNullOrWhiteSpace(input.OtherProblem))
            throw new WorkflowException("Please describe the other problem.");
        var pharmacy = page.Lookups.Pharmacies.FirstOrDefault(p => p.Id == input.PharmacyId)
            ?? throw new WorkflowException("Select a valid pharmacy.");
        var rating = new CallRating
        {
            UserId = userId, DisplayName = page.User.DisplayName, Email = page.User.Email,
            CaptureDate = clock.GetLocalNow().DateTime, Extension = page.User.Extension,
            CallDateTime = call.CallDateTime, NumberPhoned = call.CallerNumber,
            RecordingKey = string.IsNullOrWhiteSpace(call.RecordingKey) ? call.CallID : call.RecordingKey,
            Problems = string.Join("; ", problems.Select(p => p.Name)), OtherProblem = input.OtherProblem?.Trim(),
            PharmacyId = pharmacy.Id, Pharmacy = pharmacy.Name, ClientIP = clientIP, HostIP = hostIP
        };
        if (!await repository.AddAsync(rating, ct))
            return new(false, false, "This call has already been reported. No duplicate was saved.");
        Log.Information("Call rating saved");
        return await NotifyAsync(rating, ct);
    }

    private async Task<SaveResult> NotifyAsync(CallRating rating, CancellationToken ct)
    {
        try
        {
            await notifier.NotifyAsync(rating, false, ct);
            return new(true, true, "Call rating saved and help desk notified.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            Log.Warning("Rating saved but notification failed with {ErrorType}", ex.GetType().Name);
            return new(true, false, "Call rating saved, but the email notification failed. Do not submit it again; contact IT.");
        }
    }

    public async Task<string> GetRecordingAsync(Guid userId, string callId, DateOnly date, CancellationToken ct)
    {
        var call = FindCall(await GetCallsAsync(userId, date, ct), callId);
        if (string.IsNullOrWhiteSpace(call.RecordingKey)) throw new MissingRecordException("No recording is available for this call.");
        var url = await api.GetRecordingAsync(call.RecordingKey, ct)
            ?? throw new MissingRecordException("The recording is no longer available.");
        Log.Information("Operator recording lookup completed");
        return url;
    }
}

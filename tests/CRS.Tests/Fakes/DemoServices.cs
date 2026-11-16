using CRS.Core.Models;
using CRS.Core.Models.DTO;
using CRS.Core.Models.ViewModels;
using Microsoft.Extensions.Logging;

namespace CRS.Tests.Fakes;

// Test-only in-memory fixtures. Production projects never reference this assembly.
public sealed class DemoServices(TimeProvider clock, ILogger<DemoServices> logger) : IRatingRepository, IRatingNotifier
{
    public static readonly Guid OperatorId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid AdminId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private readonly object gate = new();
    private readonly List<CallRating> ratings = [];
    private bool seeded;
    private readonly Lookups lookups = new([new(1, "Poor audio"), new(2, "Dropped call"), new(3, "Other")],
        [new(1, "Demo Pharmacy North"), new(2, "Demo Pharmacy South")],
        [new(1, "Open"), new(2, "Closed"), new(3, "In progress")],
        [new(1, "Network"), new(2, "Handset"), new(3, "Other")]);
    private void Seed()
    {
        if (seeded) return;
        for (var i = 1; i <= 12; i++) ratings.Add(new()
        {
            Id = i, UserId = OperatorId, DisplayName = "Demo operator", Email = "operator@example.invalid",
            Extension = "330", CaptureDate = clock.GetLocalNow().DateTime.AddMinutes(-i),
            CallDateTime = clock.GetLocalNow().DateTime.AddDays(-1).Date.AddHours(9).AddMinutes(i),
            NumberPhoned = "DEMO-" + i, Problems = "Poor audio", PharmacyId = 1, Pharmacy = lookups.Pharmacies[0].Name,
            RecordingKey = "demo-recording-" + i, StatusId = i % 3 + 1,
            Status = lookups.Statuses.First(s => s.Id == i % 3 + 1).Name
        });
        seeded = true;
    }
    public Task<UserProfile?> GetUserAsync(Guid id, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult<UserProfile?>(id == OperatorId || id == AdminId
            ? new(id, id == AdminId ? "331" : "330", id == AdminId ? "Demo administrator" : "Demo operator", "operator@example.invalid") : null);
    }
    public Task<Lookups> GetLookupsAsync(CancellationToken ct) { ct.ThrowIfCancellationRequested(); return Task.FromResult(lookups); }
    public Task<IReadOnlyList<CallRating>> GetRatingsAsync(long? status, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        lock (gate) { Seed(); return Task.FromResult<IReadOnlyList<CallRating>>(ratings.Where(r => !status.HasValue || r.StatusId == status).OrderByDescending(r => r.CaptureDate).ToArray()); }
    }
    public Task<CallRating?> FindAsync(long id, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        lock (gate) { Seed(); return Task.FromResult(ratings.FirstOrDefault(r => r.Id == id)); }
    }
    public Task<bool> AddAsync(CallRating rating, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        lock (gate)
        {
            Seed();
            if (ratings.Any(r => r.UserId == rating.UserId && r.RecordingKey == rating.RecordingKey && r.CallDateTime == rating.CallDateTime)) return Task.FromResult(false);
            ratings.Add(rating with { Id = ratings.Max(r => r.Id) + 1 });
            return Task.FromResult(true);
        }
    }
    public Task<bool> UpdateAsync(long id, Guid userId, DateTime at, UpdateInput input, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        lock (gate)
        {
            Seed(); var index = ratings.FindIndex(r => r.Id == id);
            if (index < 0) return Task.FromResult(false);
            ratings[index] = ratings[index] with { StatusId = input.StatusId, Status = lookups.Statuses.First(s => s.Id == input.StatusId).Name,
                ReasonId = input.ReasonId, Reason = lookups.Reasons.First(r => r.Id == input.ReasonId).Name,
                ITNote = input.Note?.Trim(), UpdatedAt = at, UpdatedBy = userId };
            return Task.FromResult(true);
        }
    }
    public Task<IReadOnlyList<CallInfo>> GetCallsAsync(string extension, DateOnly date, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<CallInfo>>(Enumerable.Range(1, 6).Select(i => new CallInfo
        {
            CallID = $"demo-{date:yyyyMMdd}-{extension}-{i}", Extension = extension,
            CallDateTime = date.ToDateTime(new TimeOnly(8 + i, 15)), DurationMinutes = (i + 1).ToString(),
            IsAnswered = i != 3, Incoming = i % 2 == 0 ? "Incoming" : "Outgoing", CallerNumber = $"DEMO-000{i}",
            CallerFullName = "Demo caller", RecordingID = i == 3 ? null : $"demo-{date:yyyyMMdd}-{extension}-{i}.wav"
        }).ToArray());
    }
    public Task<string?> GetRecordingAsync(string key, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        // The test-host middleware supplies this synthetic recording.
        return Task.FromResult<string?>("/test-recording.wav");
    }
    public Task<NotificationDeliveryStatus> NotifyAsync(CallRating rating, bool updated, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        logger.LogInformation("DEMO: notification simulated; no email was sent");
        return Task.FromResult(NotificationDeliveryStatus.Sent);
    }
}

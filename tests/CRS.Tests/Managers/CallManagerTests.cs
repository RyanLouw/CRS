using CRS.Core.Managers;
using CRS.Core.Models;
using FakeItEasy;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CRS.Tests.Managers;

public sealed class CallManagerTests
{
    private readonly IRatingRepository repo = A.Fake<IRatingRepository>();
    private readonly ICallApi api = A.Fake<ICallApi>();
    private readonly IRatingNotifier notifier = A.Fake<IRatingNotifier>();
    private readonly TimeProvider clock = A.Fake<TimeProvider>();
    private readonly Guid userId = Guid.NewGuid();
    private readonly DateOnly date = new(2026, 11, 13);
    private readonly CallInfo call = new() { CallID = "call-1", Extension = "330", CallDateTime = new(2026, 11, 13, 9, 30, 0), CallerNumber = "test-number", RecordingURL = "recording.wav" };
    private readonly Lookups lookups = new([new(1, "Poor audio"), new(2, "Other")], [new(1, "Test pharmacy")], [new(1, "Open"), new(2, "Closed")], [new(1, "Network")]);
    private readonly CallManager manager;
    public CallManagerTests()
    {
        A.CallTo(() => clock.GetUtcNow()).Returns(new DateTimeOffset(2026, 11, 13, 10, 0, 0, TimeSpan.Zero));
        A.CallTo(() => clock.LocalTimeZone).Returns(TimeZoneInfo.Utc);
        A.CallTo(() => repo.GetUserAsync(userId, A<CancellationToken>._)).Returns(new UserProfile(userId, "330", "Test user", "user@example.invalid"));
        A.CallTo(() => repo.GetLookupsAsync(A<CancellationToken>._)).Returns(lookups);
        A.CallTo(() => api.GetCallsAsync("330", date, A<CancellationToken>._)).Returns(new[] { call });
        A.CallTo(() => repo.AddAsync(A<CallRating>._, A<CancellationToken>._)).Returns(true);
        manager = new(repo, api, notifier, clock, NullLogger<CallManager>.Instance);
    }
    private RatingInput Input() => new() { CallId = call.CallID, Date = date, PharmacyId = 1, ProblemIds = [1] };
    [Fact] public async Task LoadsOwnCallsAndLookupData()
    {
        var page = await manager.GetCallsAsync(userId, date, default);
        Assert.Single(page.Calls); Assert.Same(lookups, page.Lookups); Assert.Equal("330", page.User.Extension);
    }
    [Fact] public async Task EmptyApiResultProducesEmptyList()
    {
        A.CallTo(() => api.GetCallsAsync("330", date, A<CancellationToken>._)).Returns(Array.Empty<CallInfo>());
        Assert.Empty((await manager.GetCallsAsync(userId, date, default)).Calls);
    }
    [Fact] public async Task FiltersForeignExtensionAndWrongDate()
    {
        A.CallTo(() => api.GetCallsAsync("330", date, A<CancellationToken>._)).Returns(new[] { call, call with { Extension = "999" }, call with { CallDateTime = call.CallDateTime!.Value.AddDays(-1) } });
        Assert.Single((await manager.GetCallsAsync(userId, date, default)).Calls);
    }
    [Fact] public async Task MissingUserStopsBeforeApi()
    {
        A.CallTo(() => repo.GetUserAsync(userId, A<CancellationToken>._)).Returns(Task.FromResult<UserProfile?>(null));
        await Assert.ThrowsAsync<WorkflowException>(() => manager.GetCallsAsync(userId, date, default));
        A.CallTo(() => api.GetCallsAsync(A<string>._, A<DateOnly>._, A<CancellationToken>._)).MustNotHaveHappened();
    }
    [Fact] public async Task MissingExtensionStopsBeforeApi()
    {
        A.CallTo(() => repo.GetUserAsync(userId, A<CancellationToken>._)).Returns(new UserProfile(userId, " ", "", null));
        await Assert.ThrowsAsync<WorkflowException>(() => manager.GetCallsAsync(userId, date, default));
        A.CallTo(() => api.GetCallsAsync(A<string>._, A<DateOnly>._, A<CancellationToken>._)).MustNotHaveHappened();
    }
    [Fact] public async Task RejectsEmptyUserId() => await Assert.ThrowsAsync<WorkflowException>(() => manager.GetCallsAsync(Guid.Empty, date, default));
    [Theory] [InlineData(2026, 11, 14)] [InlineData(1, 1, 1)]
    public async Task RejectsInvalidDates(int year, int month, int day) => await Assert.ThrowsAsync<WorkflowException>(() => manager.GetCallsAsync(userId, new(year, month, day), default));
    [Theory] [InlineData(0)] [InlineData(99)]
    public async Task RejectsInvalidProblemBeforeSaving(long id)
    {
        var input = Input(); input.ProblemIds = id == 0 ? [] : [id];
        await Assert.ThrowsAsync<WorkflowException>(() => manager.SaveAsync(userId, input, null, null, default));
        A.CallTo(() => repo.AddAsync(A<CallRating>._, A<CancellationToken>._)).MustNotHaveHappened();
    }
    [Theory] [InlineData(null)] [InlineData("")] [InlineData(" ")]
    public async Task OtherRequiresDescription(string? note)
    {
        var input = Input(); input.ProblemIds = [2]; input.OtherProblem = note;
        await Assert.ThrowsAsync<WorkflowException>(() => manager.SaveAsync(userId, input, null, null, default));
    }
    [Fact] public async Task RejectsInactivePharmacy()
    {
        var input = Input(); input.PharmacyId = 99;
        await Assert.ThrowsAsync<WorkflowException>(() => manager.SaveAsync(userId, input, null, null, default));
    }
    [Fact] public async Task RejectsOversizedComment()
    {
        var input = Input(); input.OtherProblem = new('x', 4001);
        await Assert.ThrowsAsync<WorkflowException>(() => manager.SaveAsync(userId, input, null, null, default));
    }
    [Fact] public async Task RejectsCallIdTamperingBeforeSaving()
    {
        var input = Input(); input.CallId = "someone-elses-call";
        await Assert.ThrowsAsync<MissingRecordException>(() => manager.SaveAsync(userId, input, null, null, default));
        A.CallTo(() => repo.AddAsync(A<CallRating>._, A<CancellationToken>._)).MustNotHaveHappened();
    }
    [Fact] public async Task SavesTrustedCallDataAndDeduplicatesProblemIds()
    {
        var input = Input(); input.ProblemIds = [1, 1]; input.OtherProblem = "  details  ";
        var result = await manager.SaveAsync(userId, input, "client", "host", default);
        Assert.True(result.Saved); Assert.True(result.NotificationSent);
        A.CallTo(() => repo.AddAsync(A<CallRating>.That.Matches(r => r.UserId == userId && r.Extension == "330" &&
            r.Problems == "Poor audio" && r.NumberPhoned == call.CallerNumber && r.RecordingKey == "recording.wav" &&
            r.OtherProblem == "details" && r.ClientIP == "client" && r.CaptureDate == clock.GetUtcNow().DateTime), A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => notifier.NotifyAsync(A<CallRating>._, false, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }
    [Fact] public async Task DuplicateIsNotSavedOrEmailed()
    {
        A.CallTo(() => repo.AddAsync(A<CallRating>._, A<CancellationToken>._)).Returns(false);
        Assert.False((await manager.SaveAsync(userId, Input(), null, null, default)).Saved);
        A.CallTo(() => notifier.NotifyAsync(A<CallRating>._, A<bool>._, A<CancellationToken>._)).MustNotHaveHappened();
    }
    [Fact] public async Task DatabaseFailureNeverSendsEmail()
    {
        A.CallTo(() => repo.AddAsync(A<CallRating>._, A<CancellationToken>._)).ThrowsAsync(new DependencyException("db unavailable"));
        await Assert.ThrowsAsync<DependencyException>(() => manager.SaveAsync(userId, Input(), null, null, default));
        A.CallTo(() => notifier.NotifyAsync(A<CallRating>._, A<bool>._, A<CancellationToken>._)).MustNotHaveHappened();
    }
    [Fact] public async Task EmailFailureKeepsSavedRatingAndReturnsWarning()
    {
        A.CallTo(() => notifier.NotifyAsync(A<CallRating>._, false, A<CancellationToken>._)).ThrowsAsync(new InvalidOperationException("smtp down"));
        var result = await manager.SaveAsync(userId, Input(), null, null, default);
        Assert.True(result.Saved); Assert.False(result.NotificationSent); Assert.Contains("Do not submit", result.Message);
    }
    [Fact] public async Task ApiFailureDoesNotSave()
    {
        A.CallTo(() => api.GetCallsAsync("330", date, A<CancellationToken>._)).ThrowsAsync(new DependencyException("api down"));
        await Assert.ThrowsAsync<DependencyException>(() => manager.SaveAsync(userId, Input(), null, null, default));
        A.CallTo(() => repo.AddAsync(A<CallRating>._, A<CancellationToken>._)).MustNotHaveHappened();
    }
    [Fact] public async Task RequestCancellationPropagates()
    {
        using var cts = new CancellationTokenSource(); cts.Cancel();
        A.CallTo(() => api.GetCallsAsync("330", date, cts.Token)).ThrowsAsync(new OperationCanceledException(cts.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => manager.GetCallsAsync(userId, date, cts.Token));
    }
    [Fact] public async Task RecordingUsesServerResolvedKey()
    {
        A.CallTo(() => api.GetRecordingAsync("recording.wav", A<CancellationToken>._)).Returns("https://recordings.example.invalid/file");
        Assert.StartsWith("https://", await manager.GetRecordingAsync(userId, call.CallID, date, default));
    }
    [Fact] public async Task MissingRecordingShowsNotFound()
    {
        A.CallTo(() => api.GetRecordingAsync("recording.wav", A<CancellationToken>._)).Returns(Task.FromResult<string?>(null));
        await Assert.ThrowsAsync<MissingRecordException>(() => manager.GetRecordingAsync(userId, call.CallID, date, default));
    }
    [Fact] public async Task BlankRecordingDoesNotCallApi()
    {
        A.CallTo(() => api.GetCallsAsync("330", date, A<CancellationToken>._)).Returns(new[] { call with { RecordingURL = null } });
        await Assert.ThrowsAsync<MissingRecordException>(() => manager.GetRecordingAsync(userId, call.CallID, date, default));
        A.CallTo(() => api.GetRecordingAsync(A<string>._, A<CancellationToken>._)).MustNotHaveHappened();
    }
}

using ClosedXML.Excel;
using CRS.Core.Managers;
using CRS.Core.Models;
using FakeItEasy;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CRS.Tests.Managers;

public sealed class AdminManagerTests
{
    private readonly IRatingRepository repo = A.Fake<IRatingRepository>();
    private readonly ICallApi api = A.Fake<ICallApi>();
    private readonly IRatingNotifier notifier = A.Fake<IRatingNotifier>();
    private readonly AdminManager manager;
    private readonly CallRating rating = new() { Id = 40, RecordingKey = "saved-recording", StatusId = 1, DisplayName = "=HYPERLINK(\"bad\")" };
    private readonly Lookups lookups = new([], [], [new(1, "Open"), new(2, "Closed")], [new(1, "Network")]);
    public AdminManagerTests()
    {
        A.CallTo(() => repo.GetLookupsAsync(A<CancellationToken>._)).Returns(lookups);
        A.CallTo(() => repo.FindAsync(A<long>._, A<CancellationToken>._)).Returns(Task.FromResult<CallRating?>(null));
        A.CallTo(() => repo.FindAsync(rating.Id, A<CancellationToken>._)).Returns(rating);
        A.CallTo(() => repo.UpdateAsync(A<long>._, A<Guid>._, A<DateTime>._, A<UpdateInput>._, A<CancellationToken>._)).Returns(true);
        manager = new(repo, api, notifier, TimeProvider.System, NullLogger<AdminManager>.Instance);
    }
    private UpdateInput Input() => new() { Id = rating.Id, StatusId = 2, ReasonId = 1, Note = "Resolved" };
    [Theory] [InlineData(-10)] [InlineData(0)] [InlineData(1)] [InlineData(100)]
    public async Task EmptyRatingsStillHaveOnePage(int requested)
    {
        var page = await manager.GetPageAsync(requested, null, default);
        Assert.Equal(1, page.Page); Assert.Equal(1, page.PageCount); Assert.Empty(page.Ratings);
    }
    [Fact] public async Task PagingUsesRowsInsteadOfIdArithmeticAndClampsBeyondEnd()
    {
        var rows = Enumerable.Range(1, 12).Select(i => rating with { Id = i * 100, CaptureDate = new DateTime(2026, 1, 1).AddMinutes(i) }).ToArray();
        A.CallTo(() => repo.GetRatingsAsync(null, A<CancellationToken>._)).Returns(rows);
        var page = await manager.GetPageAsync(2, null, default);
        Assert.Equal(new long[] { 700, 600, 500, 400, 300 }, page.Ratings.Select(r => r.Id));
        Assert.Equal(3, page.PageCount);
        var last = await manager.GetPageAsync(int.MaxValue, null, default);
        Assert.Equal(3, last.Page); Assert.Equal(2, last.Ratings.Count);
    }
    [Fact] public async Task PassesStatusFilterToRepository()
    {
        await manager.GetPageAsync(1, 2, default);
        A.CallTo(() => repo.GetRatingsAsync(2, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }
    [Theory] [InlineData(-1)] [InlineData(0)] [InlineData(99)]
    public async Task InvalidStatusFilterNeverQueriesRatings(long status)
    {
        await Assert.ThrowsAsync<WorkflowException>(() => manager.GetPageAsync(1, status, default));
        A.CallTo(() => repo.GetRatingsAsync(A<long?>._, A<CancellationToken>._)).MustNotHaveHappened();
    }
    [Fact] public async Task MissingRatingIsNotFound() => await Assert.ThrowsAsync<MissingRecordException>(() => manager.GetEditAsync(999, default));
    [Theory] [InlineData(99, 1)] [InlineData(2, 99)] [InlineData(0, 0)]
    public async Task RejectsInvalidUpdateChoices(long status, long reason)
    {
        var input = Input(); input.StatusId = status; input.ReasonId = reason;
        await Assert.ThrowsAsync<WorkflowException>(() => manager.UpdateAsync(Guid.NewGuid(), input, default));
        A.CallTo(() => repo.UpdateAsync(A<long>._, A<Guid>._, A<DateTime>._, A<UpdateInput>._, A<CancellationToken>._)).MustNotHaveHappened();
    }
    [Fact] public async Task OversizedNoteIsRejected()
    {
        var input = Input(); input.Note = new('x', 4001);
        await Assert.ThrowsAsync<WorkflowException>(() => manager.UpdateAsync(Guid.NewGuid(), input, default));
    }
    [Fact] public async Task UpdateUsesTrustedRecordAndSendsNotification()
    {
        var id = Guid.NewGuid();
        Assert.True((await manager.UpdateAsync(id, Input(), default)).Saved);
        A.CallTo(() => notifier.NotifyAsync(A<CallRating>.That.Matches(r => r.Id == rating.Id && r.StatusId == 2 && r.Reason == "Network" && r.UpdatedBy == id), true, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }
    [Fact] public async Task ConcurrentDeletionDoesNotSendEmail()
    {
        A.CallTo(() => repo.UpdateAsync(A<long>._, A<Guid>._, A<DateTime>._, A<UpdateInput>._, A<CancellationToken>._)).Returns(false);
        await Assert.ThrowsAsync<MissingRecordException>(() => manager.UpdateAsync(Guid.NewGuid(), Input(), default));
        A.CallTo(() => notifier.NotifyAsync(A<CallRating>._, A<bool>._, A<CancellationToken>._)).MustNotHaveHappened();
    }
    [Fact] public async Task DatabaseFailureStopsNotification()
    {
        A.CallTo(() => repo.UpdateAsync(A<long>._, A<Guid>._, A<DateTime>._, A<UpdateInput>._, A<CancellationToken>._)).ThrowsAsync(new DependencyException("db"));
        await Assert.ThrowsAsync<DependencyException>(() => manager.UpdateAsync(Guid.NewGuid(), Input(), default));
        A.CallTo(() => notifier.NotifyAsync(A<CallRating>._, A<bool>._, A<CancellationToken>._)).MustNotHaveHappened();
    }
    [Fact] public async Task EmailFailureReportsSavedUpdate()
    {
        A.CallTo(() => notifier.NotifyAsync(A<CallRating>._, true, A<CancellationToken>._)).ThrowsAsync(new IOException("email"));
        var result = await manager.UpdateAsync(Guid.NewGuid(), Input(), default);
        Assert.True(result.Saved); Assert.False(result.NotificationSent);
    }
    [Fact] public async Task AdminRecordingUsesStoredKey()
    {
        A.CallTo(() => api.GetRecordingAsync("saved-recording", A<CancellationToken>._)).Returns("https://example.invalid/recording");
        Assert.StartsWith("https://", await manager.GetRecordingAsync(rating.Id, default));
    }
    [Fact] public async Task ExportCreatesReadableWorkbookAndNeverTreatsTextAsFormula()
    {
        A.CallTo(() => repo.GetRatingsAsync(null, A<CancellationToken>._)).Returns(new[] { rating });
        using var stream = new MemoryStream(await manager.ExportAsync(null, default));
        using var book = new XLWorkbook(stream);
        Assert.Equal("Rating ID", book.Worksheet(1).Cell(1, 1).GetString());
        Assert.Equal(rating.DisplayName, book.Worksheet(1).Cell(2, 2).GetString());
        Assert.False(book.Worksheet(1).Cell(2, 2).HasFormula);
    }
    [Fact] public async Task EmptyExportStillIncludesHeaders()
    {
        using var stream = new MemoryStream(await manager.ExportAsync(null, default));
        using var book = new XLWorkbook(stream);
        Assert.Equal(1, book.Worksheet(1).LastRowUsed()!.RowNumber());
    }
}

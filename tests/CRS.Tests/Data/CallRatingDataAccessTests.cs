using CRS.Core.DataAccess;
using CRS.Core.Models;
using CRS.Core.Models.DTO;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CRS.Tests.Data;

public sealed class CallRatingDataAccessTests : IDisposable
{
    private readonly SqliteConnection ratingsConnection = new("Data Source=:memory:");
    private readonly SqliteConnection usersConnection = new("Data Source=:memory:");
    private readonly CallRatingContext context;
    private readonly ADUsersContext users;
    private readonly CallRatingDataAccess data;
    private readonly Guid userId = Guid.NewGuid();
    public CallRatingDataAccessTests()
    {
        ratingsConnection.Open(); usersConnection.Open();
        context = new(new DbContextOptionsBuilder<CallRatingContext>().UseSqlite(ratingsConnection).Options);
        users = new(new DbContextOptionsBuilder<ADUsersContext>().UseSqlite(usersConnection).Options);
        context.Database.EnsureCreated(); users.Database.EnsureCreated();
        context.Problems.AddRange(new Problem { Id = 1, Name = "Poor audio", Active = true }, new Problem { Id = 2, Name = "Inactive", Active = false });
        context.Pharmacies.Add(new() { Id = 1, Name = "Test pharmacy", Active = true });
        context.Statuses.AddRange(new HelpDeskStatus { Id = 1, Name = "Open", Active = true }, new HelpDeskStatus { Id = 2, Name = "Closed", Active = true });
        context.Reasons.Add(new() { Id = 1, Name = "Network", Active = true });
        users.Users.Add(new() { Id = userId, DisplayName = "Test user", Extension = " 330 ", Email = "test@example.invalid" });
        context.SaveChanges(); users.SaveChanges(); context.ChangeTracker.Clear(); users.ChangeTracker.Clear();
        data = new(context, users);
    }
    private CallRating Rating(string key = "call.wav") => new()
    {
        UserId = userId, CaptureDate = new(2026, 11, 13, 10, 0, 0), Extension = "330", CallDateTime = new(2026, 11, 13, 9, 30, 0),
        RecordingKey = key, Problems = "Poor audio", PharmacyId = 1, OtherProblem = "details", NumberPhoned = "test-number"
    };
    [Fact] public async Task ReadsDirectoryThroughEfAndTrimsExtension()
    {
        var user = await data.GetUserAsync(userId, default); Assert.NotNull(user); Assert.Equal("330", user.Extension); Assert.Equal("Test user", user.DisplayName);
        Assert.Null(await data.GetUserAsync(Guid.NewGuid(), default));
    }
    [Fact] public async Task ReturnsOnlyActiveLookups()
    {
        var result = await data.GetLookupsAsync(default); Assert.Single(result.Problems); Assert.Equal("Poor audio", result.Problems[0].Name);
        Assert.Single(result.Pharmacies); Assert.Equal(2, result.Statuses.Count); Assert.Single(result.Reasons);
    }
    [Fact] public async Task InsertsWithSaveChangesAndReadsFromExistingTableMapping()
    {
        Assert.True(await data.AddAsync(Rating(), default));
        var entity = await context.CallRatings.AsNoTracking().SingleAsync();
        Assert.True(entity.CallRatingID > 0); Assert.Equal("2026-11-13", entity.CallDate); Assert.Equal("09:30:00", entity.CallTime);
        Assert.Equal(1, entity.HelpDeskStatusID); Assert.Equal("call.wav", entity.call_id);
        var read = Assert.Single(await data.GetRatingsAsync(null, default));
        Assert.Equal("Test user", read.DisplayName); Assert.Equal("Test pharmacy", read.Pharmacy); Assert.Equal("Open", read.Status);
    }
    [Fact] public async Task DuplicateInsertDoesNotCreateAnotherRow()
    {
        Assert.True(await data.AddAsync(Rating(), default)); Assert.False(await data.AddAsync(Rating(), default));
        Assert.Equal(1, await context.CallRatings.CountAsync());
    }
    [Fact] public async Task DifferentUserOrCallTimeIsNotTreatedAsDuplicate()
    {
        await data.AddAsync(Rating(), default);
        Assert.True(await data.AddAsync(Rating() with { UserId = Guid.NewGuid() }, default));
        Assert.True(await data.AddAsync(Rating() with { CallDateTime = Rating().CallDateTime!.Value.AddMinutes(1) }, default));
        Assert.Equal(3, await context.CallRatings.CountAsync());
    }
    [Fact] public async Task UpdatesStatusReasonNoteAndAuditFieldsWithEf()
    {
        await data.AddAsync(Rating(), default); var entity = await context.CallRatings.AsNoTracking().SingleAsync();
        var author = Guid.NewGuid(); var at = new DateTime(2026, 11, 13, 12, 0, 0);
        Assert.True(await data.UpdateAsync(entity.CallRatingID, author, at, new() { StatusId = 2, ReasonId = 1, Note = "  fixed  " }, default));
        var read = await data.FindAsync(entity.CallRatingID, default);
        Assert.NotNull(read); Assert.Equal("Closed", read.Status); Assert.Equal("Network", read.Reason);
        Assert.Equal("fixed", read.ITNote); Assert.Equal(author, read.UpdatedBy); Assert.Equal(at, read.UpdatedAt);
    }
    [Fact] public async Task MissingUpdateReturnsFalseAndMissingReadReturnsNull()
    {
        Assert.False(await data.UpdateAsync(999, userId, DateTime.Now, new(), default)); Assert.Null(await data.FindAsync(999, default));
    }
    [Fact] public async Task FiltersRatingsByStatus()
    {
        await data.AddAsync(Rating(), default); var id = (await context.CallRatings.SingleAsync()).CallRatingID;
        await data.UpdateAsync(id, userId, DateTime.Now, new() { StatusId = 2, ReasonId = 1 }, default);
        Assert.Empty(await data.GetRatingsAsync(1, default)); Assert.Single(await data.GetRatingsAsync(2, default));
    }
    [Fact] public async Task ReadsNullableLegacyRowsWithoutCrashing()
    {
        context.CallRatings.Add(new()); await context.SaveChangesAsync(); context.ChangeTracker.Clear();
        var row = Assert.Single(await data.GetRatingsAsync(null, default));
        Assert.Equal(Guid.Empty, row.UserId); Assert.Equal("", row.Extension); Assert.Equal("", row.RecordingKey); Assert.Null(row.CallDateTime);
    }
    [Fact] public async Task UntrustedTextIsStoredAsDataAndDoesNotExecuteSql()
    {
        var attack = "'; DROP TABLE CallRating; --";
        await data.AddAsync(Rating() with { OtherProblem = attack }, default);
        Assert.Equal(attack, (await context.CallRatings.SingleAsync()).OtherProblem); Assert.Equal(1, await context.CallRatings.CountAsync());
    }
    [Fact] public async Task DatabaseFailureBecomesSafeDependencyError()
    {
        ratingsConnection.Close(); ratingsConnection.Open();
        var error = await Assert.ThrowsAsync<DependencyException>(() => data.GetRatingsAsync(null, default));
        Assert.DoesNotContain("SELECT", error.Message);
    }
    [Fact] public async Task CancellationDoesNotWriteARating()
    {
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => data.AddAsync(Rating(), cancellation.Token));
        Assert.Equal(0, await context.CallRatings.CountAsync());
    }
    public void Dispose() { context.Dispose(); users.Dispose(); ratingsConnection.Dispose(); usersConnection.Dispose(); }
}

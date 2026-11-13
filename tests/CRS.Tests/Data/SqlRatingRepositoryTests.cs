using System.Data;
using CRS.Core.Data;
using CRS.Core.Models;
using FakeItEasy;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace CRS.Tests.Data;

public sealed class SqlRatingRepositoryTests
{
    private readonly ISqlExecutor sql = A.Fake<ISqlExecutor>();
    private readonly SqlRatingRepository repo;
    private readonly List<SqlRequest> requests = [];
    private DataTable response = new();
    public SqlRatingRepositoryTests()
    {
        A.CallTo(() => sql.QueryAsync(A<SqlRequest>._, A<CancellationToken>._)).Invokes((SqlRequest request, CancellationToken _) => requests.Add(request)).ReturnsLazily(() => Task.FromResult(response));
        repo = new(sql);
    }
    [Fact] public async Task UserLookupIsParameterizedAndUsesUsersDatabase()
    {
        var id = Guid.NewGuid(); await repo.GetUserAsync(id, default);
        var request = Assert.Single(requests); Assert.True(request.Users);
        Assert.Equal(id, request.Parameters["@UserID"]); Assert.DoesNotContain(id.ToString(), request.Text);
    }
    [Fact] public async Task MissingUserReturnsNull() => Assert.Null(await repo.GetUserAsync(Guid.NewGuid(), default));
    [Fact] public async Task NullableDirectoryFieldsDoNotCrash()
    {
        response.Columns.Add("DisplayName"); response.Columns.Add("Extension"); response.Columns.Add("Mail"); response.Rows.Add(DBNull.Value, DBNull.Value, DBNull.Value);
        var user = await repo.GetUserAsync(Guid.NewGuid(), default);
        Assert.NotNull(user); Assert.Equal("", user.Extension);
    }
    [Fact] public async Task LoadsEachActiveLookupThroughTheSuppliedProcedure()
    {
        var result = await repo.GetLookupsAsync(default);
        Assert.Empty(result.Problems); Assert.Equal(4, requests.Count);
        Assert.All(requests, r => { Assert.True(r.StoredProcedure); Assert.Equal("CSP_GetDropdownList", r.Text); });
        Assert.Equal(new[] { "Problems", "Pharmacy", "HelpDeskStatus", "HelpDeskReason" }, requests.Select(r => r.Parameters["@DropdownType"]));
    }
    [Fact] public async Task FilterUsesNullableSqlParameter()
    {
        await repo.GetRatingsAsync(null, default); await repo.GetRatingsAsync(2, default);
        Assert.Null(requests[0].Parameters["@HelpDeskStatusFilter"]); Assert.Equal(2L, requests[1].Parameters["@HelpDeskStatusFilter"]);
    }
    [Fact] public async Task MapsLegacyAliasesAndNullColumns()
    {
        response.Columns.Add("CallRatingID", typeof(long)); response.Columns.Add("Number Dialed");
        response.Columns.Add("CallDateTime", typeof(DateTime)); response.Rows.Add(12L, "test-number", DBNull.Value);
        var rating = Assert.Single(await repo.GetRatingsAsync(null, default));
        Assert.Equal(12L, rating.Id); Assert.Equal("test-number", rating.NumberPhoned); Assert.Null(rating.CallDateTime);
    }
    [Fact] public async Task LookupByIdIsParameterized()
    {
        Assert.Null(await repo.FindAsync(42, default)); Assert.Equal(42L, Assert.Single(requests).Parameters["@Id"]);
    }
    [Theory] [InlineData(true)] [InlineData(false)]
    public async Task InsertReportsDatabaseDuplicateOutcomeAndBindsUntrustedValues(bool saved)
    {
        response.Columns.Add("Saved", typeof(bool)); response.Rows.Add(saved);
        var rating = new CallRating { UserId = Guid.NewGuid(), Problems = "'; DROP TABLE CallRating; --", RecordingKey = "file.wav", OtherProblem = null };
        Assert.Equal(saved, await repo.AddAsync(rating, default));
        var request = Assert.Single(requests);
        Assert.Equal(rating.Problems, request.Parameters["@Problems"]); Assert.DoesNotContain(rating.Problems, request.Text);
        Assert.Null(request.Parameters["@OtherProblem"]);
        Assert.Contains("UPDLOCK, HOLDLOCK", request.Text); Assert.Contains("BEGIN TRANSACTION", request.Text);
        Assert.Contains("ROLLBACK", request.Text); Assert.Contains("CSP_AddCallRating", request.Text);
    }
    [Theory] [InlineData(true)] [InlineData(false)]
    public async Task UpdateUsesCurrentUserAndHandlesConcurrentDeletion(bool saved)
    {
        response.Columns.Add("Saved", typeof(bool)); response.Rows.Add(saved);
        var userId = Guid.NewGuid(); var date = new DateTime(2026, 11, 13);
        Assert.Equal(saved, await repo.UpdateAsync(8, userId, date, new() { StatusId = 2, ReasonId = 1, Note = "  details  " }, default));
        var p = Assert.Single(requests).Parameters;
        Assert.Equal(userId, p["@ITUpdateByUserID"]); Assert.Equal(date, p["@ITUpdateDate"]); Assert.Equal("details", p["@ITNote"]);
    }
    [Fact] public async Task DatabaseFailurePropagatesToManager()
    {
        A.CallTo(() => sql.QueryAsync(A<SqlRequest>._, A<CancellationToken>._)).ThrowsAsync(new DependencyException("database down"));
        await Assert.ThrowsAsync<DependencyException>(() => repo.GetRatingsAsync(null, default));
    }
    [Fact] public async Task UnconfiguredDatabaseHasActionableFailureWithoutNetwork()
    {
        var executor = new SqlExecutor(Options.Create(new DatabaseOptions()), NullLogger<SqlExecutor>.Instance);
        await Assert.ThrowsAsync<DependencyException>(() => executor.QueryAsync(new("SELECT 1", false, new Dictionary<string, object?>()), default));
    }
}

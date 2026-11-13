using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using CRS.Core.Demo;
using CRS.Core.Models;
using CRS.Security;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CRS.Tests;

public sealed class IdentityAndDemoTests
{
    [Theory]
    [InlineData("oid")]
    [InlineData("http://schemas.microsoft.com/identity/claims/objectidentifier")]
    [InlineData(ClaimTypes.NameIdentifier)]
    public void RecognizesDirectoryUserIdClaim(string claim)
    {
        var id = Guid.NewGuid(); var user = new ClaimsPrincipal(new ClaimsIdentity([new(claim, id.ToString())], "test"));
        Assert.Equal(id, UserContext.UserId(user));
    }
    [Theory] [InlineData("")] [InlineData("not-a-guid")] [InlineData("00000000-0000-0000-0000-000000000000")]
    public void InvalidUserIdCannotBecomeAnotherUsersAccount(string value)
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity([new("oid", value)], "test"));
        Assert.Throws<WorkflowException>(() => UserContext.UserId(user));
    }
    [Fact] public void MissingUserIdIsRejected() => Assert.Throws<WorkflowException>(() => UserContext.UserId(new ClaimsPrincipal()));
    [Fact] public void InvalidRatingTriggersBindingValidation()
    {
        var results = new List<ValidationResult>();
        var input = new RatingInput();
        Assert.False(Validator.TryValidateObject(input, new ValidationContext(input), results, true));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(RatingInput.CallId)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(RatingInput.PharmacyId)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(RatingInput.ProblemIds)));
    }
    [Fact] public async Task DemoConcurrentSubmissionsSaveOnlyOneRating()
    {
        var demo = new DemoServices(TimeProvider.System, NullLogger<DemoServices>.Instance);
        var rating = new CallRating { UserId = DemoServices.OperatorId, RecordingKey = "concurrent-test", CallDateTime = DateTime.Today };
        var results = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Task.Run(() => demo.AddAsync(rating, default))));
        Assert.Single(results, saved => saved);
        Assert.Single(await demo.GetRatingsAsync(null, default), r => r.RecordingKey == rating.RecordingKey);
    }
    [Fact] public async Task DemoDoesNotShareChangesAcrossInstances()
    {
        var first = new DemoServices(TimeProvider.System, NullLogger<DemoServices>.Instance);
        var second = new DemoServices(TimeProvider.System, NullLogger<DemoServices>.Instance);
        await first.AddAsync(new() { RecordingKey = "new-test", UserId = DemoServices.OperatorId }, default);
        Assert.DoesNotContain(await second.GetRatingsAsync(null, default), r => r.RecordingKey == "new-test");
    }
}

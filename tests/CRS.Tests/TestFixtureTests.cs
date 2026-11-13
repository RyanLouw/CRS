using System.ComponentModel.DataAnnotations;
using CRS.Core.Models;
using CRS.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CRS.Tests;

public sealed class TestFixtureTests
{
    [Fact] public void InvalidRatingTriggersBindingValidation()
    {
        var results = new List<ValidationResult>(); var input = new RatingInput();
        Assert.False(Validator.TryValidateObject(input, new ValidationContext(input), results, true));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(RatingInput.CallId)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(RatingInput.PharmacyId)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(RatingInput.ProblemIds)));
    }
    [Fact] public async Task ConcurrentTestSubmissionsSaveOnlyOneRating()
    {
        var demo = new DemoServices(TimeProvider.System, NullLogger<DemoServices>.Instance);
        var rating = new CallRating { UserId = DemoServices.OperatorId, RecordingKey = "concurrent-test", CallDateTime = DateTime.Today };
        var results = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Task.Run(() => demo.AddAsync(rating, default))));
        Assert.Single(results, saved => saved);
        Assert.Single(await demo.GetRatingsAsync(null, default), r => r.RecordingKey == rating.RecordingKey);
    }
    [Fact] public void FakeServicesAreOnlyInTheTestAssembly() => Assert.NotEqual(typeof(CallRating).Assembly, typeof(DemoServices).Assembly);
}

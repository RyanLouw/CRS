using System.ComponentModel.DataAnnotations;
using CRS.Core.Models.DTO;
using CRS.Core.Models.ViewModels;
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
        Assert.DoesNotContain(results, r => r.MemberNames.Contains(nameof(RatingInput.PharmacyId)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(RatingInput.ProblemIds)));
    }

    [Fact] public void LegacyOptionalFieldsAndLongTextPassBindingValidation()
    {
        var rating = new RatingInput { CallId = new string('c', 600), ProblemIds = [1], OtherProblem = new string('x', 5001) };
        Assert.True(Validator.TryValidateObject(rating, new ValidationContext(rating), [], true));
        var update = new UpdateInput { Id = 1, StatusId = 2, ReasonId = 0, Note = new string('x', 5001) };
        Assert.True(Validator.TryValidateObject(update, new ValidationContext(update), [], true));
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

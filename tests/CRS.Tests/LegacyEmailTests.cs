using CRS.Core.Models;
using CRS.Core.Models.DTO;
using CRS.Core.Notifications;
using FakeItEasy;
using Microsoft.Extensions.Options;
using Xunit;

namespace CRS.Tests;

public sealed class LegacyEmailTests
{
    private readonly CallRating rating = new()
    {
        Extension = "330", DisplayName = "Test User", NumberPhoned = "0123456789",
        CallDate = "2026-10-05", CallTime = "09:30", Pharmacy = "Test Pharmacy",
        Problems = "Poor audio; Other", OtherProblem = "Details", StatusId = 2,
        Status = "Closed", Reason = "Resolved", ITNote = "Fixed", Email = "user@example.test",
        ClientIP = "198.51.100.7", HostIP = "10.0.0.1"
    };

    [Fact]
    public async Task UsesAllThreeLegacySubjectsAndDistinctTemplates()
    {
        var sender = A.Fake<IEmailSender>();
        var notifier = new SmtpRatingNotifier(sender, Options.Create(new SmtpOptions { HelpDesk = "desk@example.test" }));
        await notifier.NotifyAsync(rating, false, default);
        await notifier.NotifyAsync(rating, true, default);
        var messages = Fake.GetCalls(sender).Select(c => (NotificationMessage)c.Arguments[0]!).ToArray();
        Assert.Equal(3, messages.Length);
        Assert.Equal("Call Rating For Ext: 330", messages[0].Subject);
        Assert.Equal("Status Of Call Rating For Ext: 330", messages[1].Subject);
        Assert.Equal("Call Feedback : 0123456789 at 09:30 2026-10-05", messages[2].Subject);
        Assert.Contains("Please see below details of call:", messages[0].Html);
        Assert.Contains("Please see status of HelpDesk on below call:", messages[1].Html);
        Assert.Contains("Our support representative has reviewed", messages[2].Html);
        Assert.Contains("198.51.100.7", messages[0].Html);
        Assert.Contains("10.0.0.1", messages[1].Html);
        Assert.DoesNotContain("198.51.100.7", messages[2].Html);
        Assert.DoesNotContain("{HelpDeskReason}", messages[2].Html);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void OmitsEmptyOtherRowAndEncodesAllUntrustedValues(bool updated, bool feedback)
    {
        var html = RatingEmailTemplates.Render(rating with
        {
            OtherProblem = null, DisplayName = "<script>alert(1)</script>",
            Problems = "<img src=x>; Audio", ITNote = "<script>bad</script>"
        }, updated, feedback);
        Assert.DoesNotContain("Other Problems:", html);
        Assert.DoesNotContain("<script>", html);
        Assert.DoesNotContain("<img", html);
        Assert.Contains("&lt;img", html);
        Assert.DoesNotContain("{Other}", html);
    }

    [Fact]
    public void DoesNotExpandPlaceholdersInsideUserText()
    {
        var html = RatingEmailTemplates.Render(rating with { DisplayName = "{ITNote}", ITNote = "secret-note" }, false);
        Assert.Contains("{ITNote}", html);
        Assert.DoesNotContain("secret-note", html);
    }

    [Fact]
    public async Task FeedbackIsAttemptedWhenOnlyTheHelpDeskMessageFails()
    {
        var sender = A.Fake<IEmailSender>();
        A.CallTo(() => sender.SendAsync(A<NotificationMessage>.That.Matches(m => m.To == "desk@example.test"), A<CancellationToken>._))
            .ThrowsAsync(new IOException("test SMTP error"));
        var notifier = new SmtpRatingNotifier(sender, Options.Create(new SmtpOptions { HelpDesk = "desk@example.test" }));
        await Assert.ThrowsAsync<DependencyException>(() => notifier.NotifyAsync(rating, true, default));
        A.CallTo(() => sender.SendAsync(A<NotificationMessage>.That.Matches(m => m.To == "user@example.test"), A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }
}

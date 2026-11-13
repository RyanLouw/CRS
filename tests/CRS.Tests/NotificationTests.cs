using CRS.Core.Models;
using CRS.Core.Notifications;
using FakeItEasy;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace CRS.Tests;

public sealed class NotificationTests
{
    private readonly IEmailSender sender = A.Fake<IEmailSender>();
    private readonly SmtpRatingNotifier notifier;
    public NotificationTests() => notifier = new(sender, Options.Create(new SmtpOptions { HelpDesk = "helpdesk@example.invalid" }), NullLogger<SmtpRatingNotifier>.Instance);
    [Theory] [InlineData(false, 1, 1)] [InlineData(true, 1, 1)] [InlineData(true, 2, 2)]
    public async Task SendsHelpDeskAndOnlySendsUserFeedbackOnClosure(bool updated, long status, int expected)
    {
        await notifier.NotifyAsync(new() { StatusId = status, Email = "user@example.invalid" }, updated, default);
        A.CallTo(() => sender.SendAsync(A<NotificationMessage>._, A<CancellationToken>._)).MustHaveHappened(expected, Times.Exactly);
        A.CallTo(() => sender.SendAsync(A<NotificationMessage>.That.Matches(m => m.To == "helpdesk@example.invalid"), A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }
    [Fact] public async Task MissingUserEmailStillNotifiesHelpDesk()
    {
        await notifier.NotifyAsync(new() { StatusId = 2, Email = null }, true, default);
        A.CallTo(() => sender.SendAsync(A<NotificationMessage>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }
    [Fact] public async Task EncodesUntrustedHtmlInEmail()
    {
        await notifier.NotifyAsync(new() { OtherProblem = "<script>alert(1)</script>", ITNote = "<img src=x>" }, false, default);
        A.CallTo(() => sender.SendAsync(A<NotificationMessage>.That.Matches(m => m.Html.Contains("&lt;script&gt;") && !m.Html.Contains("<script>") && !m.Html.Contains("<img")), A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }
    [Fact] public async Task HelpDeskDeliveryFailurePropagatesWithoutUserEmail()
    {
        A.CallTo(() => sender.SendAsync(A<NotificationMessage>._, A<CancellationToken>._)).ThrowsAsync(new IOException("SMTP"));
        await Assert.ThrowsAsync<IOException>(() => notifier.NotifyAsync(new() { StatusId = 2, Email = "user@example.invalid" }, true, default));
        A.CallTo(() => sender.SendAsync(A<NotificationMessage>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }
    [Fact] public async Task MissingHelpDeskRecipientDoesNotAttemptDelivery()
    {
        var missing = new SmtpRatingNotifier(sender, Options.Create(new SmtpOptions()), NullLogger<SmtpRatingNotifier>.Instance);
        await Assert.ThrowsAsync<DependencyException>(() => missing.NotifyAsync(new(), false, default));
        A.CallTo(() => sender.SendAsync(A<NotificationMessage>._, A<CancellationToken>._)).MustNotHaveHappened();
    }
    [Fact] public async Task MissingSmtpConfigurationFailsWithoutNetwork()
    {
        var smtp = new SmtpEmailSender(Options.Create(new SmtpOptions()));
        await Assert.ThrowsAsync<DependencyException>(() => smtp.SendAsync(new("test@example.invalid", "Test", "Test"), default));
    }
}

using System.Text.Encodings.Web;
using CRS.Core.Models;
using CRS.Core.Models.DTO;
using Serilog;
using Microsoft.Extensions.Options;

namespace CRS.Core.Notifications;

public sealed class SmtpRatingNotifier(IEmailSender sender, IOptions<SmtpOptions> options) : IRatingNotifier
{
    public async Task<NotificationDeliveryStatus> NotifyAsync(CallRating rating, bool updated, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var settings = options.Value;
        settings.ValidateDeliveryMode();
        if (settings.DebugEmail == 2)
        {
            Log.Information("Rating notification suppressed by email configuration; update {IsUpdate}", updated);
            return NotificationDeliveryStatus.Disabled;
        }
        static string E(object? value) => HtmlEncoder.Default.Encode(value?.ToString() ?? "");
        var html = $"<h1>Call rating{(updated ? " update" : "")}</h1>" +
            $"<p>User: {E(rating.DisplayName)}</p><p>Extension: {E(rating.Extension)}</p>" +
            $"<p>Call: {E(rating.CallDateTime)} / {E(rating.NumberPhoned)}</p>" +
            $"<p>Pharmacy: {E(rating.Pharmacy)}</p><p>Problems: {E(rating.Problems)}</p>" +
            $"<p>Other: {E(rating.OtherProblem)}</p><p>Status: {E(rating.Status)}</p>" +
            $"<p>Reason: {E(rating.Reason)}</p><p>IT note: {E(rating.ITNote)}</p>";
        var helpDesk = settings.DebugEmail == 1 ? settings.DebugEmailAddress : settings.HelpDesk;
        if (string.IsNullOrWhiteSpace(helpDesk)) throw new DependencyException("The help-desk email recipient is not configured.");
        await sender.SendAsync(new(helpDesk, updated ? "Call rating status update" : "New call rating", html), ct);
        if (updated && rating.StatusId == 2 && !string.IsNullOrWhiteSpace(rating.Email))
            await sender.SendAsync(new(settings.DebugEmail == 1 ? settings.DebugEmailAddress : rating.Email, "Call rating feedback", html), ct);
        Log.Information("Rating notification delivery completed; update {IsUpdate}; debug {IsDebug}", updated, settings.DebugEmail == 1);
        return settings.DebugEmail == 1 ? NotificationDeliveryStatus.Redirected : NotificationDeliveryStatus.Sent;
    }
}

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
        var helpDesk = settings.DebugEmail == 1 ? settings.DebugEmailAddress : settings.HelpDesk;
        if (string.IsNullOrWhiteSpace(helpDesk)) throw new DependencyException("The help-desk email recipient is not configured.");
        var subject = updated ? "Status Of Call Rating For Ext: " + rating.Extension.Trim()
            : "Call Rating For Ext: " + rating.Extension.Trim();
        Exception? failure = null;
        try { await sender.SendAsync(new(helpDesk, subject, RatingEmailTemplates.Render(rating, updated)), ct); }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            failure = ex;
            Log.Warning("Help-desk email delivery failed with {ErrorType}", ex.GetType().Name);
        }
        // The legacy workflow attempts user feedback independently of help-desk delivery.
        if (updated && rating.StatusId == 2 && !string.IsNullOrWhiteSpace(rating.Email))
        {
            var recipient = settings.DebugEmail == 1 ? settings.DebugEmailAddress : rating.Email;
            var date = rating.CallDate ?? rating.CallDateTime?.ToString("yyyy-MM-dd");
            var time = rating.CallTime ?? rating.CallDateTime?.ToString("HH:mm:ss");
            try
            {
                await sender.SendAsync(new(recipient, $"Call Feedback : {rating.NumberPhoned.Trim()} at {time} {date}",
                    RatingEmailTemplates.Render(rating, updated, userFeedback: true)), ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
            {
                failure = ex;
                Log.Warning("User feedback email delivery failed with {ErrorType}", ex.GetType().Name);
            }
        }
        if (failure is not null) throw new DependencyException("One or more email notifications could not be delivered.");
        Log.Information("Rating notification delivery completed; update {IsUpdate}; debug {IsDebug}", updated, settings.DebugEmail == 1);
        return settings.DebugEmail == 1 ? NotificationDeliveryStatus.Redirected : NotificationDeliveryStatus.Sent;
    }
}

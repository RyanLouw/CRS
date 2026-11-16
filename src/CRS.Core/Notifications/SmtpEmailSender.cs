using System.Net;
using System.Net.Mail;
using CRS.Core.Models;
using CRS.Core.Models.DTO;
using Microsoft.Extensions.Options;
using Serilog;

namespace CRS.Core.Notifications;

public sealed class SmtpEmailSender(IOptions<SmtpOptions> options) : IEmailSender
{
    public async Task SendAsync(NotificationMessage message, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var settings = options.Value;
        settings.ValidateDeliveryMode();
        if (settings.DebugEmail == 2)
        {
            Log.Information("SMTP delivery suppressed by email configuration");
            return;
        }
        var from = string.IsNullOrWhiteSpace(settings.From) ? settings.Username : settings.From;
        if (string.IsNullOrWhiteSpace(settings.Host) || string.IsNullOrWhiteSpace(from))
            throw new DependencyException("Email delivery has not been configured.");
        if (settings.Port is < 1 or > 65535 || settings.TimeoutSeconds is < 1 or > 3600)
            throw new DependencyException("Configure a valid SMTP port and a timeout between 1 and 3600 seconds.");
        var hasUsername = !string.IsNullOrWhiteSpace(settings.Username);
        var hasPassword = !string.IsNullOrEmpty(settings.Password);
        if (hasUsername != hasPassword)
            throw new DependencyException("Configure both Smtp:Username and Smtp:Password, or leave both empty for an unauthenticated relay.");

        var recipient = settings.DebugEmail == 1 ? settings.DebugEmailAddress : message.To;
        var subject = settings.DebugEmail == 1 ? "[DEBUG] " + message.Subject : message.Subject;
        using var mail = new MailMessage(from, recipient, subject, message.Html) { IsBodyHtml = true };
        using var client = new SmtpClient(settings.Host, settings.Port)
        {
            EnableSsl = settings.EnableSsl,
            UseDefaultCredentials = false
        };
        if (hasUsername) client.Credentials = new NetworkCredential(settings.Username, settings.Password);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(settings.TimeoutSeconds));
        await client.SendMailAsync(mail, timeout.Token);
    }
}

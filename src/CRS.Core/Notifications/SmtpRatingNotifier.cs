using System.Net;
using System.Net.Mail;
using System.Text.Encodings.Web;
using CRS.Core.Models;
using Serilog;
using Microsoft.Extensions.Options;

namespace CRS.Core.Notifications;

public sealed class SmtpOptions
{
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public string From { get; set; } = "";
    public string HelpDesk { get; set; } = "";
    public string? Username { get; set; }
    public string? Password { get; set; }
}
public sealed record NotificationMessage(string To, string Subject, string Html);
public interface IEmailSender
{
    Task SendAsync(NotificationMessage message, CancellationToken cancellationToken);
}
public sealed class SmtpEmailSender(IOptions<SmtpOptions> options) : IEmailSender
{
    public async Task SendAsync(NotificationMessage message, CancellationToken ct)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.Host) || string.IsNullOrWhiteSpace(settings.From))
            throw new DependencyException("Email delivery has not been configured.");
        using var client = new SmtpClient(settings.Host, settings.Port) { EnableSsl = true };
        if (!string.IsNullOrWhiteSpace(settings.Username)) client.Credentials = new NetworkCredential(settings.Username, settings.Password);
        using var mail = new MailMessage(settings.From, message.To, message.Subject, message.Html) { IsBodyHtml = true };
        await client.SendMailAsync(mail, ct);
    }
}
public sealed class SmtpRatingNotifier(IEmailSender sender, IOptions<SmtpOptions> options) : IRatingNotifier
{
    public async Task NotifyAsync(CallRating rating, bool updated, CancellationToken ct)
    {
        static string E(object? value) => HtmlEncoder.Default.Encode(value?.ToString() ?? "");
        var html = $"<h1>Call rating{(updated ? " update" : "")}</h1>" +
            $"<p>User: {E(rating.DisplayName)}</p><p>Extension: {E(rating.Extension)}</p>" +
            $"<p>Call: {E(rating.CallDateTime)} / {E(rating.NumberPhoned)}</p>" +
            $"<p>Pharmacy: {E(rating.Pharmacy)}</p><p>Problems: {E(rating.Problems)}</p>" +
            $"<p>Other: {E(rating.OtherProblem)}</p><p>Status: {E(rating.Status)}</p>" +
            $"<p>Reason: {E(rating.Reason)}</p><p>IT note: {E(rating.ITNote)}</p>";
        if (string.IsNullOrWhiteSpace(options.Value.HelpDesk)) throw new DependencyException("The help-desk email recipient is not configured.");
        await sender.SendAsync(new(options.Value.HelpDesk, updated ? "Call rating status update" : "New call rating", html), ct);
        if (updated && rating.StatusId == 2 && !string.IsNullOrWhiteSpace(rating.Email))
            await sender.SendAsync(new(rating.Email, "Call rating feedback", html), ct);
        Log.Information("Rating notification delivery completed; update {IsUpdate}", updated);
    }
}

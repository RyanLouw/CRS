using CRS.Core.Models;

namespace CRS.Core.Notifications;

public sealed class SmtpOptions
{
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public string From { get; set; } = "";
    public string HelpDesk { get; set; } = "";
    public string? Username { get; set; }
    public string? Password { get; set; }
    public bool EnableSsl { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 300;
    // Matches legacy DebugEmail: 0 = live, 1 = debug recipient, 2 = no delivery.
    public int DebugEmail { get; set; }
    public string DebugEmailAddress { get; set; } = "";

    public void ValidateDeliveryMode()
    {
        if (DebugEmail is < 0 or > 2)
            throw new DependencyException("Smtp:DebugEmail must be 0 (live), 1 (debug address), or 2 (disabled).");
        if (DebugEmail == 1 && string.IsNullOrWhiteSpace(DebugEmailAddress))
            throw new DependencyException("Configure Smtp:DebugEmailAddress before using debug email delivery.");
    }
}

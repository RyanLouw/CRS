using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using CRS.Core.Models.DTO;

namespace CRS.Core.Notifications;

public static class RatingEmailTemplates
{
    private static readonly IReadOnlyDictionary<string, string> templates = new[]
    {
        "HelpDeskEmailBody", "HelpDeskStatusEmailBody", "HelpDeskCallRatingStatusEmailToUser", "ReplaceOtherSection"
    }.ToDictionary(name => name, name =>
    {
        using var stream = typeof(RatingEmailTemplates).Assembly.GetManifestResourceStream(
            $"CRS.Core.Notifications.Templates.{name}.html")
            ?? throw new InvalidOperationException("A required rating email template is missing.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    });

    public static string Render(CallRating rating, bool updated, bool userFeedback = false)
    {
        var template = templates[userFeedback ? "HelpDeskCallRatingStatusEmailToUser" : updated ? "HelpDeskStatusEmailBody" : "HelpDeskEmailBody"];
        if (string.IsNullOrWhiteSpace(rating.OtherProblem)) template = template.Replace(templates["ReplaceOtherSection"], "");
        static string Encode(string? value) => HtmlEncoder.Default.Encode(value ?? "");
        var problems = rating.Problems.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var values = new Dictionary<string, string>
        {
            ["DisplayName"] = Encode(rating.DisplayName), ["Ext"] = Encode(rating.Extension),
            ["CallDate"] = Encode(rating.CallDate ?? rating.CallDateTime?.ToString("yyyy-MM-dd")),
            ["CallTime"] = Encode(rating.CallTime ?? rating.CallDateTime?.ToString("HH:mm:ss")),
            ["NumberDialed"] = Encode(rating.NumberPhoned), ["Pharmacy"] = Encode(rating.Pharmacy),
            ["Ticked"] = string.Join(updated || userFeedback ? "; <br/>" : "; ", problems.Select(Encode)),
            ["Other"] = Encode(rating.OtherProblem), ["HelpDeskStatus"] = Encode(rating.Status),
            ["HelpDeskReason"] = Encode(rating.Reason), ["ITNote"] = Encode(rating.ITNote),
            ["IP"] = Encode(rating.ClientIP), ["HostIP"] = Encode(rating.HostIP)
        };
        // Replace once so user text containing a placeholder cannot be expanded again.
        return Regex.Replace(template, @"\{(\w+)\}", match => values.GetValueOrDefault(match.Groups[1].Value, match.Value));
    }
}

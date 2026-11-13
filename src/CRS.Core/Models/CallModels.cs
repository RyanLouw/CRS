using System.ComponentModel.DataAnnotations;

namespace CRS.Core.Models;

public sealed record CallInfo
{
    public string CallID { get; init; } = "";
    public string Extension { get; init; } = "";
    public DateTime? CallDateTime { get; init; }
    public string DurationMinutes { get; init; } = "";
    public bool IsAnswered { get; init; }
    public string Incoming { get; init; } = "";
    public string? RecordingURL { get; init; }
    public string? RecordingID { get; init; }
    public string CallerNumber { get; init; } = "";
    public string CallerFullName { get; init; } = "";
    public string RecordingKey => !string.IsNullOrWhiteSpace(RecordingURL) ? RecordingURL : RecordingID ?? "";
}

public sealed record UserProfile(Guid Id, string Extension, string DisplayName, string? Email);
public sealed record Lookup(long Id, string Name);
public sealed record Lookups(IReadOnlyList<Lookup> Problems, IReadOnlyList<Lookup> Pharmacies,
    IReadOnlyList<Lookup> Statuses, IReadOnlyList<Lookup> Reasons);

public sealed record CallRating
{
    public long Id { get; init; }
    public Guid UserId { get; init; }
    public string DisplayName { get; init; } = "";
    public string? Email { get; init; }
    public DateTime CaptureDate { get; init; }
    public string Extension { get; init; } = "";
    public DateTime? CallDateTime { get; init; }
    public string NumberPhoned { get; init; } = "";
    public string Problems { get; init; } = "";
    public string? OtherProblem { get; init; }
    // Legacy call_id holds the recording filename; preserve it for old recordings.
    public string RecordingKey { get; init; } = "";
    public string? ClientIP { get; init; }
    public string? HostIP { get; init; }
    public long? PharmacyId { get; init; }
    public string Pharmacy { get; init; } = "";
    public long StatusId { get; init; } = 1;
    public string Status { get; init; } = "Open";
    public long? ReasonId { get; init; }
    public string Reason { get; init; } = "";
    public string? ITNote { get; init; }
    public Guid? UpdatedBy { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public sealed class RatingInput
{
    [Required, StringLength(500)] public string CallId { get; set; } = "";
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    [MinLength(1, ErrorMessage = "Select at least one problem.")]
    public long[] ProblemIds { get; set; } = [];
    [StringLength(4000)] public string? OtherProblem { get; set; }
    [Range(1, long.MaxValue, ErrorMessage = "Select a pharmacy.")] public long PharmacyId { get; set; }
}

public sealed class UpdateInput
{
    [Range(1, long.MaxValue)] public long Id { get; set; }
    [Range(1, long.MaxValue)] public long StatusId { get; set; }
    [Range(1, long.MaxValue)] public long ReasonId { get; set; }
    [StringLength(4000)] public string? Note { get; set; }
}

public sealed record SaveResult(bool Saved, bool NotificationSent, string Message);
public sealed record CallListPage(UserProfile User, DateOnly Date, IReadOnlyList<CallInfo> Calls, Lookups Lookups);
public sealed record RatingPage(CallInfo Call, Lookups Lookups, RatingInput Input);
public sealed record AdminPage(IReadOnlyList<CallRating> Ratings, Lookups Lookups, int Page, int PageCount, long? StatusId);
public sealed record AdminEditPage(CallRating Rating, Lookups Lookups, UpdateInput Input);
public sealed class WorkflowException(string message) : Exception(message);
public sealed class MissingRecordException(string message) : Exception(message);
public sealed class DependencyException(string message) : Exception(message);

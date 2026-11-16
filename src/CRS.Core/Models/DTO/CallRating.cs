namespace CRS.Core.Models.DTO;

public sealed record CallRating
{
    public long Id { get; init; }
    public Guid UserId { get; init; }
    public string DisplayName { get; init; } = "";
    public string? Email { get; init; }
    public DateTime CaptureDate { get; init; }
    public string Extension { get; init; } = "";
    public DateTime? CallDateTime { get; init; }
    public string? CallDate { get; init; }
    public string? CallTime { get; init; }
    public bool? WiFi { get; init; }
    public bool? LAN { get; init; }
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

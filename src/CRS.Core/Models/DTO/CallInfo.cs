namespace CRS.Core.Models.DTO;

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

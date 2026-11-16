namespace CRS.Core.DataAccess;

public sealed class CallApiOptions
{
    public int Debug { get; set; }
    public string DebugExtension { get; set; } = "330";
    public DateOnly DebugDate { get; set; } = new(2026, 10, 5);
}

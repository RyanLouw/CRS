namespace CRS.Core.Models;

public sealed class Problem
{
    public long Id { get; set; }
    public string? Name { get; set; }
    public bool? Active { get; set; }
}
public sealed class Pharmacy
{
    public long Id { get; set; }
    public string? Name { get; set; }
    public bool? Active { get; set; }
}
public sealed class HelpDeskStatus
{
    public long Id { get; set; }
    public string? Name { get; set; }
    public bool? Active { get; set; }
}
public sealed class HelpDeskReason
{
    public long Id { get; set; }
    public string? Name { get; set; }
    public bool? Active { get; set; }
}
public sealed class DirectoryUser
{
    public Guid Id { get; set; }
    public string? Extension { get; set; }
    public string? DisplayName { get; set; }
    public string? Email { get; set; }
}

// Nullable columns match the supplied existing SQL schema.
public sealed class CallRatingEntity
{
    public long CallRatingID { get; set; }
    public Guid? UserID { get; set; }
    public DateTime? CaptureDate { get; set; }
    public string? Ext { get; set; }
    public string? CallDate { get; set; }
    public string? CallTime { get; set; }
    public string? NumberPhoned { get; set; }
    public string? Problems { get; set; }
    public string? OtherProblem { get; set; }
    public DateTime? CallDateTime { get; set; }
    public bool? WiFi { get; set; }
    public bool? LAN { get; set; }
    public string? ClientIP { get; set; }
    public string? HostIP { get; set; }
    public string? call_id { get; set; }
    public Guid? ITUpdateByUserID { get; set; }
    public DateTime? ITUpdateDate { get; set; }
    public long? HelpDeskStatusID { get; set; }
    public string? ITNote { get; set; }
    public long? HelpDeskReasonID { get; set; }
    public long? PharmacyID { get; set; }
}

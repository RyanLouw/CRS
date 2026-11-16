namespace CRS.Core.Models.DTO;

public sealed record SaveResult(bool Saved, bool NotificationSent, string Message);

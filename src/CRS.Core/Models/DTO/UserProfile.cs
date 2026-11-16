namespace CRS.Core.Models.DTO;

public sealed record UserProfile(Guid Id, string Extension, string DisplayName, string? Email);

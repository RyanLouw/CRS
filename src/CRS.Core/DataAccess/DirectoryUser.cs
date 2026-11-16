namespace CRS.Core.DataAccess;

public sealed class DirectoryUser
{
    public Guid Id { get; set; }
    public string? Extension { get; set; }
    public string? DisplayName { get; set; }
    public string? Email { get; set; }
}

// Nullable columns match the supplied existing SQL schema.

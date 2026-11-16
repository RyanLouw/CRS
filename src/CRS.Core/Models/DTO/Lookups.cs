namespace CRS.Core.Models.DTO;

public sealed record Lookups(IReadOnlyList<Lookup> Problems, IReadOnlyList<Lookup> Pharmacies,
    IReadOnlyList<Lookup> Statuses, IReadOnlyList<Lookup> Reasons);

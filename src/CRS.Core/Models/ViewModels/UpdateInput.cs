using System.ComponentModel.DataAnnotations;

namespace CRS.Core.Models.ViewModels;

public sealed class UpdateInput
{
    [Range(1, long.MaxValue)] public long Id { get; set; }
    [Range(1, long.MaxValue)] public long StatusId { get; set; }
    [Range(0, long.MaxValue)] public long ReasonId { get; set; }
    public string? Note { get; set; }
}

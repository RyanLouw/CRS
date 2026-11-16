using System.ComponentModel.DataAnnotations;

namespace CRS.Core.Models.ViewModels;

public sealed class RatingInput
{
    [Required, StringLength(500)] public string CallId { get; set; } = "";
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    [MinLength(1, ErrorMessage = "Select at least one problem.")]
    public long[] ProblemIds { get; set; } = [];
    [StringLength(4000)] public string? OtherProblem { get; set; }
    [Range(1, long.MaxValue, ErrorMessage = "Select a pharmacy.")] public long PharmacyId { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace CRS.Core.Models.ViewModels;

public sealed class RatingInput
{
    [Required] public string CallId { get; set; } = "";
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    [MinLength(1, ErrorMessage = "Select at least one problem.")]
    public long[] ProblemIds { get; set; } = [];
    public string? OtherProblem { get; set; }
    [Range(0, long.MaxValue, ErrorMessage = "Select a valid pharmacy.")] public long PharmacyId { get; set; }
}

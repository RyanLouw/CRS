using System.ComponentModel.DataAnnotations;
using CRS.Core.Models.DTO;

namespace CRS.Core.Models.ViewModels;

public sealed record CallListPage(UserProfile User, DateOnly Date, IReadOnlyList<CallInfo> Calls, Lookups Lookups)
{
    public IReadOnlySet<string> ReportedCallIds { get; init; } = new HashSet<string>();
}

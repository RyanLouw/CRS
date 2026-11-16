using System.ComponentModel.DataAnnotations;
using CRS.Core.Models.DTO;

namespace CRS.Core.Models.ViewModels;

public sealed record AdminPage(IReadOnlyList<CallRating> Ratings, Lookups Lookups, int Page, int PageCount, long? StatusId);

using System.ComponentModel.DataAnnotations;
using CRS.Core.Models.DTO;

namespace CRS.Core.Models.ViewModels;

public sealed record RatingPage(CallInfo Call, Lookups Lookups, RatingInput Input);

using System.ComponentModel.DataAnnotations;
using CRS.Core.Models.DTO;

namespace CRS.Core.Models.ViewModels;

public sealed record AdminEditPage(CallRating Rating, Lookups Lookups, UpdateInput Input);

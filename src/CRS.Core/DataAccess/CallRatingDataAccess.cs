using System.Data;
using System.Data.Common;
using CRS.Core.Models;
using CRS.Core.Models.DTO;
using CRS.Core.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace CRS.Core.DataAccess;

public sealed class CallRatingDataAccess(CallRatingContext context, ADUsersContext users) : IRatingRepository
{
    public Task<UserProfile?> GetUserAsync(Guid id, CancellationToken ct) => Run(async () =>
    {
        var user = await users.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == id, ct);
        return user is null ? null : new UserProfile(user.Id, user.Extension?.Trim() ?? "", user.DisplayName ?? "", user.Email);
    });
    public Task<Lookups> GetLookupsAsync(CancellationToken ct) => Run(async () => new Lookups(
        await context.Problems.AsNoTracking().Where(x => x.Active == true).OrderBy(x => x.Id).Select(x => new Lookup(x.Id, x.Name ?? "")).ToListAsync(ct),
        await context.Pharmacies.AsNoTracking().Where(x => x.Active == true).OrderBy(x => x.Id).Select(x => new Lookup(x.Id, x.Name ?? "")).ToListAsync(ct),
        await context.Statuses.AsNoTracking().Where(x => x.Active == true).OrderBy(x => x.Id).Select(x => new Lookup(x.Id, x.Name ?? "")).ToListAsync(ct),
        await context.Reasons.AsNoTracking().Where(x => x.Active == true).OrderBy(x => x.Id).Select(x => new Lookup(x.Id, x.Name ?? "")).ToListAsync(ct)));
    public Task<IReadOnlyList<CallRating>> GetRatingsAsync(long? status, CancellationToken ct) => Run<IReadOnlyList<CallRating>>(async () =>
    {
        var rows = await context.CallRatings.AsNoTracking().Where(r => !status.HasValue || r.HelpDeskStatusID == status)
            .OrderByDescending(r => r.CaptureDate).ThenByDescending(r => r.CallRatingID).ToListAsync(ct);
        return await Describe(rows.Select(Map).ToArray(), ct);
    });
    public Task<CallRating?> FindAsync(long id, CancellationToken ct) => Run(async () =>
    {
        var rating = await context.CallRatings.AsNoTracking().SingleOrDefaultAsync(r => r.CallRatingID == id, ct);
        return rating is null ? null : (await Describe([Map(rating)], ct)).Single();
    });
    private async Task<IReadOnlyList<CallRating>> Describe(IReadOnlyList<CallRating> rows, CancellationToken ct)
    {
        if (rows.Count == 0) return rows;
        var ids = rows.Select(r => r.UserId).Distinct().ToArray();
        var names = await users.Users.AsNoTracking().Where(u => ids.Contains(u.Id)).ToDictionaryAsync(u => u.Id, ct);
        var pharmacies = await context.Pharmacies.AsNoTracking().ToDictionaryAsync(p => p.Id, ct);
        var statuses = await context.Statuses.AsNoTracking().ToDictionaryAsync(p => p.Id, ct);
        var reasons = await context.Reasons.AsNoTracking().ToDictionaryAsync(p => p.Id, ct);
        return rows.Select(r => r with
        {
            DisplayName = names.GetValueOrDefault(r.UserId)?.DisplayName ?? "", Email = names.GetValueOrDefault(r.UserId)?.Email,
            Pharmacy = pharmacies.GetValueOrDefault(r.PharmacyId ?? 0)?.Name ?? "",
            Status = statuses.GetValueOrDefault(r.StatusId)?.Name ?? "",
            Reason = reasons.GetValueOrDefault(r.ReasonId ?? 0)?.Name ?? ""
        }).ToArray();
    }
    public Task<bool> AddAsync(CallRating rating, CancellationToken ct) => Run(async () =>
    {
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        if (await context.CallRatings.AnyAsync(r => r.UserID == rating.UserId && r.call_id == rating.RecordingKey && r.CallDateTime == rating.CallDateTime, ct))
        {
            await transaction.CommitAsync(ct);
            return false;
        }
        var entity = new CallRatingEntity
        {
            UserID = rating.UserId, CaptureDate = rating.CaptureDate, Ext = rating.Extension,
            CallDate = rating.CallDateTime?.ToString("yyyy-MM-dd"), CallTime = rating.CallDateTime?.ToString("HH:mm:ss"),
            NumberPhoned = rating.NumberPhoned, Problems = rating.Problems, OtherProblem = rating.OtherProblem,
            CallDateTime = rating.CallDateTime, WiFi = rating.WiFi, LAN = rating.LAN,
            ClientIP = rating.ClientIP, HostIP = rating.HostIP, call_id = rating.RecordingKey,
            HelpDeskStatusID = 1, PharmacyID = rating.PharmacyId
        };
        context.CallRatings.Add(entity);
        await context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        context.Entry(entity).State = EntityState.Detached;
        Log.Information("Call rating written through EF Core");
        return true;
    });
    public Task<bool> UpdateAsync(long id, Guid userId, DateTime at, UpdateInput input, CancellationToken ct) => Run(async () =>
    {
        var rating = await context.CallRatings.SingleOrDefaultAsync(r => r.CallRatingID == id, ct);
        if (rating is null) return false;
        rating.HelpDeskStatusID = input.StatusId;
        rating.HelpDeskReasonID = input.ReasonId;
        rating.ITNote = input.Note?.Trim();
        rating.ITUpdateByUserID = userId;
        rating.ITUpdateDate = at;
        try { await context.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { context.Entry(rating).State = EntityState.Detached; return false; }
        context.Entry(rating).State = EntityState.Detached;
        Log.Information("Help-desk update written through EF Core");
        return true;
    });
    private static CallRating Map(CallRatingEntity r) => new()
    {
        Id = r.CallRatingID, UserId = r.UserID ?? Guid.Empty, CaptureDate = r.CaptureDate ?? DateTime.MinValue,
        Extension = r.Ext ?? "", CallDateTime = r.CallDateTime, CallDate = r.CallDate, CallTime = r.CallTime,
        NumberPhoned = r.NumberPhoned ?? "", Problems = r.Problems ?? "", OtherProblem = r.OtherProblem,
        RecordingKey = r.call_id ?? "", ClientIP = r.ClientIP, HostIP = r.HostIP, WiFi = r.WiFi, LAN = r.LAN,
        PharmacyId = r.PharmacyID, StatusId = r.HelpDeskStatusID ?? 0, ReasonId = r.HelpDeskReasonID,
        ITNote = r.ITNote, UpdatedBy = r.ITUpdateByUserID, UpdatedAt = r.ITUpdateDate
    };
    private static async Task<T> Run<T>(Func<Task<T>> action)
    {
        try { return await action(); }
        catch (Exception ex) when (ex is DbException or DbUpdateException)
        {
            Log.Error("EF Core database operation failed with {ErrorType}", ex.GetType().Name);
            throw new DependencyException("The database operation failed. Please try again or contact IT.");
        }
    }
}

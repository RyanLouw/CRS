namespace CRS.Core.Models;

public interface ICallApi
{
    Task<IReadOnlyList<CallInfo>> GetCallsAsync(string extension, DateOnly date, CancellationToken cancellationToken);
    Task<string?> GetRecordingAsync(string key, CancellationToken cancellationToken);
}
public interface IRatingRepository
{
    Task<UserProfile?> GetUserAsync(Guid userId, CancellationToken cancellationToken);
    Task<Lookups> GetLookupsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<CallRating>> GetRatingsAsync(long? statusId, CancellationToken cancellationToken);
    Task<CallRating?> FindAsync(long id, CancellationToken cancellationToken);
    Task<bool> AddAsync(CallRating rating, CancellationToken cancellationToken);
    Task<bool> UpdateAsync(long id, Guid userId, DateTime updatedAt, UpdateInput input, CancellationToken cancellationToken);
}
public interface IRatingNotifier
{
    Task NotifyAsync(CallRating rating, bool updated, CancellationToken cancellationToken);
}
public interface ICallManager
{
    Task<CallListPage> GetCallsAsync(Guid userId, DateOnly date, CancellationToken cancellationToken);
    Task<RatingPage> GetRatingPageAsync(Guid userId, string callId, DateOnly date, CancellationToken cancellationToken);
    Task<SaveResult> SaveAsync(Guid userId, RatingInput input, string? clientIP, string? hostIP, CancellationToken cancellationToken);
    Task<string> GetRecordingAsync(Guid userId, string callId, DateOnly date, CancellationToken cancellationToken);
}
public interface IAdminManager
{
    Task<AdminPage> GetPageAsync(int page, long? statusId, CancellationToken cancellationToken);
    Task<AdminEditPage> GetEditAsync(long id, CancellationToken cancellationToken);
    Task<SaveResult> UpdateAsync(Guid userId, UpdateInput input, CancellationToken cancellationToken);
    Task<string> GetRecordingAsync(long id, CancellationToken cancellationToken);
    Task<byte[]> ExportAsync(long? statusId, CancellationToken cancellationToken);
}

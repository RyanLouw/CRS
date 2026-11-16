namespace CRS.Services;

public interface IUserSession
{
    Guid? GetCurrentUserId();
    string GetUserName();
    string? GetCurrentUserEmail();
}

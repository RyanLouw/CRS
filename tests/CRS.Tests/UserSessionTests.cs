using System.Security.Claims;
using CRS.Services;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace CRS.Tests;

public sealed class UserSessionTests
{
    [Fact]
    public void MissingHttpContextHasNoUser()
    {
        var session = new UserSession(new HttpContextAccessor());
        Assert.Null(session.GetCurrentUserId());
        Assert.Null(session.GetCurrentUserEmail());
        Assert.Equal("Unknown", session.GetUserName());
    }

    [Fact]
    public void AnonymousClaimsAreNotUsed()
    {
        var session = Session(new ClaimsIdentity([
            new Claim("oid", Guid.NewGuid().ToString()),
            new Claim("name", "Anonymous"), new Claim("email", "anonymous@example.test")]));
        Assert.Null(session.GetCurrentUserId());
        Assert.Null(session.GetCurrentUserEmail());
        Assert.Equal("Unknown", session.GetUserName());
    }

    [Theory]
    [InlineData("oid")]
    [InlineData("http://schemas.microsoft.com/identity/claims/objectidentifier")]
    [InlineData(ClaimTypes.NameIdentifier)]
    public void ReadsAuthenticatedDirectoryId(string claimType)
    {
        var id = Guid.NewGuid();
        var session = Session(new ClaimsIdentity([new Claim(claimType, id.ToString())], "Test"));
        Assert.Equal(id, session.GetCurrentUserId());
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public void InvalidDirectoryIdReturnsNull(string value)
    {
        var session = Session(new ClaimsIdentity([new Claim("oid", value)], "Test"));
        Assert.Null(session.GetCurrentUserId());
    }

    [Fact]
    public void EntraObjectIdTakesPrecedenceOverSubjectIdentifier()
    {
        var id = Guid.NewGuid();
        var session = Session(new ClaimsIdentity([
            new Claim("oid", id.ToString()), new Claim(ClaimTypes.NameIdentifier, "opaque-subject")], "Test"));
        Assert.Equal(id, session.GetCurrentUserId());
    }

    [Theory]
    [InlineData("name")]
    [InlineData(ClaimTypes.Name)]
    public void ReadsDisplayName(string claimType)
    {
        var session = Session(new ClaimsIdentity([new Claim(claimType, "Test User")], "Test"));
        Assert.Equal("Test User", session.GetUserName());
    }

    [Fact]
    public void CombinesGivenNameAndSurname()
    {
        var session = Session(new ClaimsIdentity([
            new Claim(ClaimTypes.GivenName, "Test"), new Claim(ClaimTypes.Surname, "User")], "Test"));
        Assert.Equal("Test User", session.GetUserName());
    }

    [Theory]
    [InlineData(ClaimTypes.Email)]
    [InlineData("email")]
    [InlineData("preferred_username")]
    [InlineData("upn")]
    [InlineData(ClaimTypes.Upn)]
    [InlineData(ClaimTypes.Name)]
    public void ReadsEmailWithIdentityFallbacks(string claimType)
    {
        var session = Session(new ClaimsIdentity([new Claim(claimType, "user@example.test")], "Test"));
        Assert.Equal("user@example.test", session.GetCurrentUserEmail());
    }

    [Fact]
    public void ReadsEachRequestWithoutCachingThePreviousUser()
    {
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        var session = new UserSession(accessor);
        var id = Guid.NewGuid();
        accessor.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("oid", id.ToString())], "Test"));
        Assert.Equal(id, session.GetCurrentUserId());
        accessor.HttpContext = new DefaultHttpContext();
        Assert.Null(session.GetCurrentUserId());
    }

    private static UserSession Session(ClaimsIdentity identity) => new(new HttpContextAccessor
    {
        HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
    });
}

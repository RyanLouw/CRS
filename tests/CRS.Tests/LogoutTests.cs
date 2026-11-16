using System.Net;
using System.Text.RegularExpressions;
using CRS.Tests.Fakes;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Xunit;

namespace CRS.Tests;

public sealed class LogoutTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("invalid-directory-id")]
    public async Task LogoutClearsCookieAndRedirectsThroughEntraToSignedOutPage(string? userId)
    {
        using var factory = new TestApplicationFactory(services =>
            services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
                options.Configuration = new OpenIdConnectConfiguration
                {
                    EndSessionEndpoint = "https://login.microsoftonline.com/22222222-2222-2222-2222-222222222222/oauth2/v2.0/logout"
                }), userId: userId);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false
        });
        var page = await client.GetStringAsync("/Home/SignedOut");
        Assert.Contains("Log Out", page);
        var token = Regex.Match(page, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        Assert.NotEmpty(token);
        using var response = await client.PostAsync("/account/logout", new FormUrlEncodedContent(
            new Dictionary<string, string> { ["__RequestVerificationToken"] = WebUtility.HtmlDecode(token) }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains(response.Headers.GetValues("Set-Cookie"), c => c.Contains(".AspNetCore.Cookies=;") && c.Contains("expires="));
        var location = Assert.IsType<Uri>(response.Headers.Location);
        Assert.Equal("login.microsoftonline.com", location.Host);
        var query = QueryHelpers.ParseQuery(location.Query);
        Assert.Equal("https://localhost/signout-callback-oidc", query["post_logout_redirect_uri"].ToString());
        using var callback = await client.GetAsync("/signout-callback-oidc?state=" + Uri.EscapeDataString(query["state"].ToString()));
        Assert.Equal(HttpStatusCode.Redirect, callback.StatusCode);
        Assert.Equal("/Home/SignedOut", callback.Headers.Location?.OriginalString);
        using var signedOut = await client.GetAsync(callback.Headers.Location);
        Assert.Contains("You have been signed out", await signedOut.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task LogoutRequiresAntiforgeryValidation()
    {
        using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        using var response = await client.PostAsync("/account/logout", new StringContent(""));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetDoesNotSignOutTheUser()
    {
        using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/account/logout");
        Assert.True(response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.MethodNotAllowed);
        Assert.False(response.Headers.Contains("Set-Cookie"));
    }

    [Fact]
    public async Task FailedSignInShowsGenericErrorWithoutProviderDetails()
    {
        using var factory = new TestApplicationFactory(authenticated: false);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false
        });
        using var response = await client.GetAsync("/signin-oidc?error=access_denied&error_description=private-provider-detail");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Home/AuthenticationError", response.Headers.Location?.OriginalString);
        using var page = await client.GetAsync(response.Headers.Location);
        Assert.Equal(HttpStatusCode.Unauthorized, page.StatusCode);
        var html = await page.Content.ReadAsStringAsync();
        Assert.Contains("Sign-in could not be completed", html);
        Assert.DoesNotContain("private-provider-detail", html);
    }
}

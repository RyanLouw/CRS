using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Net;
using CRS.Tests.Fakes;
using Xunit;

namespace CRS.Tests;

public sealed class AuthenticationTests
{
    [Fact]
    public async Task AnonymousProductionChallengeRedirectsToEntra()
    {
        using var factory = new TestApplicationFactory(services =>
        {
            services.PostConfigure<AuthenticationOptions>(options =>
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme);
            // Fixed discovery data avoids external Entra access while exercising the real OIDC handler.
            services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
                options.Configuration = new OpenIdConnectConfiguration
                {
                    AuthorizationEndpoint = "https://login.microsoftonline.com/22222222-2222-2222-2222-222222222222/oauth2/v2.0/authorize"
                });
        }, authenticated: false);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false
        });
        using var response = await client.GetAsync("/CallList");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = Assert.IsType<Uri>(response.Headers.Location);
        Assert.Equal("login.microsoftonline.com", location.Host);
        var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(location.Query);
        Assert.Equal("11111111-1111-1111-1111-111111111111", query["client_id"].ToString());
        Assert.Equal("https://localhost/signin-oidc", query["redirect_uri"].ToString());
        Assert.Equal("code", query["response_type"].ToString());
    }

    [Theory]
    [InlineData("/CallList")]
    [InlineData("/Admin")]
    public async Task AnonymousRequestsAreRejected(string path)
    {
        using var factory = new TestApplicationFactory(authenticated: false);
        using var client = factory.CreateClient();
        using var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ApplicationUserCanSeeCallsWithoutAdminNavigation()
    {
        using var factory = new TestApplicationFactory(roles: []);
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/CallList");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("My calls", html);
        Assert.DoesNotContain(">Help desk</a>", html);
    }

    [Theory]
    [InlineData("GET", "/Admin")]
    [InlineData("GET", "/Admin/Edit/1")]
    [InlineData("POST", "/Admin/Edit/1")]
    [InlineData("GET", "/Admin/Recording/1")]
    [InlineData("GET", "/Admin/Export")]
    public async Task NonAdminSeesAccessDeniedForEveryAdminAction(string method, string path)
    {
        using var factory = new TestApplicationFactory(roles: []);
        using var client = factory.CreateClient();
        using var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("You do not have permission to view this page.", html);
        Assert.Contains(path, html);
        Assert.Contains("Go to Home", html);
        var home = System.Text.RegularExpressions.Regex.Match(html, "<a[^>]*href=\"([^\"]+)\"[^>]*>\\s*Go to Home");
        Assert.True(home.Success);
        using var homeResponse = await client.GetAsync(home.Groups[1].Value);
        Assert.Equal(HttpStatusCode.OK, homeResponse.StatusCode);
        Assert.Contains("My calls", await homeResponse.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("OtherRole")]
    [InlineData("callratingsystemadmin")]
    public async Task WrongRoleDoesNotGrantAdminAccess(string role)
    {
        using var factory = new TestApplicationFactory(roles: [role]);
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/Admin");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminRoleGrantsAccessAndNavigation()
    {
        using var factory = new TestApplicationFactory(roles: ["CallRatingSystemAdmin"]);
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/Admin");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(">Help desk</a>", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task AccessDeniedPageIsPublicAndDoesNotDisplayExternalReturnUrls()
    {
        using var factory = new TestApplicationFactory(authenticated: false);
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/Home/AccessDenied?returnUrl=https%3A%2F%2Fexample.test%2F");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Access Denied", html);
        Assert.DoesNotContain("example.test", html);
    }

    [Fact]
    public async Task MissingPageRemainsNotFound()
    {
        using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/missing-page");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.DoesNotContain("You do not have permission", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task InvalidDirectoryIdentityIsForbidden(string userId)
    {
        using var factory = new TestApplicationFactory(userId: userId);
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/CallList");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
    [Theory]
    [InlineData("CallRatingSystemPSP", true)]
    [InlineData("CallRatingSystemIT", true)]
    [InlineData("CallRatingSystemAdmin", true)]
    [InlineData("CallRatingSystemUser", false)]
    public async Task RecordingAccessMatchesLegacyRoleConvention(string role, bool allowed)
    {
        using var factory = new TestApplicationFactory(roles: [role]);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false
        });
        using var page = await client.GetAsync("/CallList");
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        var html = await page.Content.ReadAsStringAsync();
        Assert.Equal(allowed, html.Contains(">Recording</a>"));
        var date = DateOnly.FromDateTime(DateTime.Today);
        using var response = await client.GetAsync($"/CallList/Recording?callId=demo-{date:yyyyMMdd}-330-1&date={date:yyyy-MM-dd}");
        Assert.Equal(allowed ? HttpStatusCode.Redirect : HttpStatusCode.Forbidden, response.StatusCode);
        if (allowed) Assert.Equal("/test-recording.wav", response.Headers.Location?.OriginalString);
    }

}

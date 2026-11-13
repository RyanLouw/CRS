using System.Net;
using System.Text.RegularExpressions;
using CRS.Core.Models;
using FakeItEasy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace CRS.Tests;

public sealed class WorkflowWebTests
{
    private static WebApplicationFactory<Program> Factory(Action<IServiceCollection>? configure = null) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development").UseSetting("Demo:Enabled", "true")
                .UseSetting("Logging:Directory", Path.Combine(Path.GetTempPath(), "crs-workflow-tests"));
            if (configure != null) builder.ConfigureServices(configure);
        });
    private static HttpClient Client(WebApplicationFactory<Program> factory) => factory.CreateClient(new WebApplicationFactoryClientOptions
        { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false, HandleCookies = true });
    private static string Field(string html, string name)
    {
        var tag = Regex.Matches(html, "<input[^>]+>").Select(m => m.Value).First(t => t.Contains("name=\"" + name + "\""));
        return WebUtility.HtmlDecode(Regex.Match(tag, "value=\"([^\"]*)\"").Groups[1].Value);
    }
    private static string Token(string html) => Field(html, "__RequestVerificationToken");
    private static async Task Login(HttpClient client, string role)
    {
        var html = await client.GetStringAsync("/Account/Login");
        using var response = await client.PostAsync("/Account/DemoLogin", new FormUrlEncodedContent(new Dictionary<string, string>
            { ["role"] = role, ["__RequestVerificationToken"] = Token(html) }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }
    private static async Task<(string Html, string Url)> RatingForm(HttpClient client)
    {
        var calls = await client.GetStringAsync("/Calls");
        var url = WebUtility.HtmlDecode(Regex.Match(calls, "href=\"([^\"]*/Calls/Rate[^\"]*)\"").Groups[1].Value);
        Assert.NotEmpty(url);
        return (await client.GetStringAsync(url), url);
    }
    private static Dictionary<string, string> RatingFields(string html) => new()
    {
        ["__RequestVerificationToken"] = Token(html), ["Input.CallId"] = Field(html, "Input.CallId"),
        ["Input.Date"] = Field(html, "Input.Date"), ["Input.ProblemIds"] = "1", ["Input.PharmacyId"] = "1",
        ["Input.OtherProblem"] = "test details"
    };
    [Theory] [InlineData("/Calls")] [InlineData("/Admin")] [InlineData("/Admin/Export")]
    public async Task AnonymousUsersAreChallenged(string url)
    {
        using var factory = Factory(); using var client = Client(factory); using var response = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode); Assert.Contains("/Account/Login", response.Headers.Location!.ToString());
    }
    [Fact] public async Task LoginRequiresAntiforgeryToken()
    {
        using var factory = Factory(); using var client = Client(factory);
        using var response = await client.PostAsync("/Account/DemoLogin", new FormUrlEncodedContent(new Dictionary<string, string> { ["role"] = "Admin" }));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    [Fact] public async Task OperatorCanSeeCallsButCannotAdministerOrDownloadRecordings()
    {
        using var factory = Factory(); using var client = Client(factory); await Login(client, "Operator");
        var calls = await client.GetStringAsync("/Calls"); Assert.Contains("DEMO MODE", calls); Assert.Contains("Report problem", calls);
        foreach (var route in new[] { "/Admin", "/Admin/Export", "/Calls/Recording?callId=test", "/demo/recording" })
        {
            using var response = await client.GetAsync(route);
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode); Assert.Contains("/Account/Denied", response.Headers.Location!.ToString());
        }
    }
    [Fact] public async Task RejectsUnexpectedDemoRole()
    {
        using var factory = Factory(); using var client = Client(factory);
        var html = await client.GetStringAsync("/Account/Login");
        using var response = await client.PostAsync("/Account/DemoLogin", new FormUrlEncodedContent(new Dictionary<string, string>
            { ["role"] = "SuperAdmin", ["__RequestVerificationToken"] = Token(html) }));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    [Fact] public async Task SavesRatingAndPreventsDuplicateSubmission()
    {
        using var factory = Factory(); using var client = Client(factory); await Login(client, "Operator");
        var (html, url) = await RatingForm(client);
        using var saved = await client.PostAsync("/Calls/Rate", new FormUrlEncodedContent(RatingFields(html)));
        Assert.Equal(HttpStatusCode.Redirect, saved.StatusCode);
        Assert.Contains("Call rating saved", await client.GetStringAsync(saved.Headers.Location!));
        html = await client.GetStringAsync(url);
        using var duplicate = await client.PostAsync("/Calls/Rate", new FormUrlEncodedContent(RatingFields(html)));
        Assert.Equal(HttpStatusCode.Redirect, duplicate.StatusCode);
        Assert.Contains("already been reported", await client.GetStringAsync(duplicate.Headers.Location!));
    }
    [Fact] public async Task MissingProblemsReturnsFormWithValidationAndPreservesComment()
    {
        using var factory = Factory(); using var client = Client(factory); await Login(client, "Operator");
        var (html, _) = await RatingForm(client); var fields = RatingFields(html); fields.Remove("Input.ProblemIds");
        using var response = await client.PostAsync("/Calls/Rate", new FormUrlEncodedContent(fields));
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode); Assert.Contains("Select at least one problem", body); Assert.Contains("test details", body);
    }
    [Fact] public async Task RatingPostRequiresAntiforgeryToken()
    {
        using var factory = Factory(); using var client = Client(factory); await Login(client, "Operator");
        var (html, _) = await RatingForm(client); var fields = RatingFields(html); fields.Remove("__RequestVerificationToken");
        using var response = await client.PostAsync("/Calls/Rate", new FormUrlEncodedContent(fields));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    [Fact] public async Task AdminCanFilterPageEditAndExport()
    {
        using var factory = Factory(); using var client = Client(factory); await Login(client, "Admin");
        var page = await client.GetStringAsync("/Admin?page=2"); Assert.Contains("Page 2 of 3", page);
        var filter = await client.GetStringAsync("/Admin?statusId=2"); Assert.Contains("Page 1 of 1", filter);
        var edit = await client.GetStringAsync("/Admin/Edit/1");
        using var saved = await client.PostAsync("/Admin/Edit", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = Token(edit), ["Input.Id"] = "1", ["Input.StatusId"] = "2",
            ["Input.ReasonId"] = "1", ["Input.Note"] = "Resolved by test"
        }));
        Assert.Equal(HttpStatusCode.Redirect, saved.StatusCode);
        Assert.Contains("Resolved by test", await client.GetStringAsync("/Admin/Edit/1"));
        using var report = await client.GetAsync("/Admin/Export");
        Assert.Equal(HttpStatusCode.OK, report.StatusCode);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", report.Content.Headers.ContentType!.MediaType);
        Assert.True((await report.Content.ReadAsByteArrayAsync()).Length > 1000);
    }
    [Fact] public async Task PspCanDownloadSyntheticRecording()
    {
        using var factory = Factory(); using var client = Client(factory); await Login(client, "PSP");
        var calls = await client.GetStringAsync("/Calls");
        var recording = WebUtility.HtmlDecode(Regex.Match(calls, "href=\"([^\"]*/Calls/Recording[^\"]*)\"").Groups[1].Value);
        using var response = await client.GetAsync(recording); Assert.Equal("/demo/recording", response.Headers.Location!.ToString());
        using var wav = await client.GetAsync(response.Headers.Location); Assert.Equal("audio/wav", wav.Content.Headers.ContentType!.MediaType);
        Assert.Equal("RIFF", System.Text.Encoding.ASCII.GetString((await wav.Content.ReadAsByteArrayAsync())[..4]));
    }
    [Theory] [InlineData("/Admin/Edit/999", 404)] [InlineData("/Admin?statusId=999", 400)]
    public async Task AdminBadRequestsReturnUsefulStatus(string url, int status)
    {
        using var factory = Factory(); using var client = Client(factory); await Login(client, "Admin");
        using var response = await client.GetAsync(url); Assert.Equal(status, (int)response.StatusCode);
        Assert.Contains("Reference:", await response.Content.ReadAsStringAsync());
    }
    [Fact] public async Task DependencyFailureReturns503WithoutInternalDetails()
    {
        var manager = A.Fake<ICallManager>();
        A.CallTo(() => manager.GetCallsAsync(A<Guid>._, A<DateOnly>._, A<CancellationToken>._)).ThrowsAsync(new DependencyException("Calls service is unavailable."));
        using var factory = Factory(s => { s.RemoveAll<ICallManager>(); s.AddSingleton(manager); }); using var client = Client(factory);
        await Login(client, "Operator"); using var response = await client.GetAsync("/Calls");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode); Assert.Contains("Calls service is unavailable", await response.Content.ReadAsStringAsync());
    }
    [Fact] public async Task UnexpectedFailureIsSanitized()
    {
        var manager = A.Fake<ICallManager>();
        A.CallTo(() => manager.GetCallsAsync(A<Guid>._, A<DateOnly>._, A<CancellationToken>._)).ThrowsAsync(new InvalidOperationException("private-secret-connection-string"));
        using var factory = Factory(s => { s.RemoveAll<ICallManager>(); s.AddSingleton(manager); }); using var client = Client(factory);
        await Login(client, "Operator"); using var response = await client.GetAsync("/Calls");
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.DoesNotContain("private-secret", await response.Content.ReadAsStringAsync());
    }
    [Fact] public async Task DemoCannotStartOutsideDevelopment()
    {
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.UseEnvironment("Production").UseSetting("Demo:Enabled", "true"));
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => Task.Run(() => factory.CreateClient()));
        Assert.Contains("Demo mode is allowed only", error.Message);
    }
    [Fact] public async Task SignOutClearsAccess()
    {
        using var factory = Factory(); using var client = Client(factory); await Login(client, "Operator");
        var html = await client.GetStringAsync("/Calls");
        using var response = await client.PostAsync("/Account/Logout", new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = Token(html) }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        using var calls = await client.GetAsync("/Calls"); Assert.Contains("/Account/Login", calls.Headers.Location!.ToString());
    }
}

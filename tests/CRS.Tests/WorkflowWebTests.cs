using System.Net;
using System.Text.RegularExpressions;
using CRS.Core.Models;
using CRS.Tests.Fakes;
using FakeItEasy;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace CRS.Tests;

public sealed class WorkflowWebTests
{
    private static HttpClient Client(WebApplicationFactory<Program> factory) => factory.CreateClient(new WebApplicationFactoryClientOptions
        { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false, HandleCookies = true });
    private static string Field(string html, string name)
    {
        var tag = Regex.Matches(html, "<input[^>]+>").Select(m => m.Value).First(t => t.Contains("name=\"" + name + "\""));
        return WebUtility.HtmlDecode(Regex.Match(tag, "value=\"([^\"]*)\"").Groups[1].Value);
    }
    private static string Token(string html) => Field(html, "__RequestVerificationToken");
    private static async Task<(string Html, string Url)> RatingForm(HttpClient client)
    {
        var calls = await client.GetStringAsync("/CallList");
        var url = WebUtility.HtmlDecode(Regex.Match(calls, "href=\"([^\"]*/CallList/Rate[^\"]*)\"").Groups[1].Value);
        Assert.NotEmpty(url); return (await client.GetStringAsync(url), url);
    }
    private static Dictionary<string, string> RatingFields(string html) => new()
    {
        ["__RequestVerificationToken"] = Token(html), ["Input.CallId"] = Field(html, "Input.CallId"),
        ["Input.Date"] = Field(html, "Input.Date"), ["Input.ProblemIds"] = "1", ["Input.PharmacyId"] = "1", ["Input.OtherProblem"] = "test details"
    };
    [Fact] public void ApplicationHasWorkflowAndAccessDeniedControllers()
    {
        var controllers = typeof(Program).Assembly.GetTypes().Where(t => !t.IsAbstract && typeof(Microsoft.AspNetCore.Mvc.Controller).IsAssignableFrom(t)).Select(t => t.Name).Order().ToArray();
        Assert.Equal(new[] { "AdminController", "CallListController", "HomeController" }, controllers);
    }
    [Fact] public async Task SavesRatingAndPreventsDuplicateSubmission()
    {
        using var factory = new TestApplicationFactory(); using var client = Client(factory);
        var (html, url) = await RatingForm(client);
        using var saved = await client.PostAsync("/CallList/Rate", new FormUrlEncodedContent(RatingFields(html)));
        Assert.Equal(HttpStatusCode.Redirect, saved.StatusCode); Assert.Contains("Call rating saved", await client.GetStringAsync(saved.Headers.Location!));
        html = await client.GetStringAsync(url);
        using var duplicate = await client.PostAsync("/CallList/Rate", new FormUrlEncodedContent(RatingFields(html)));
        Assert.Contains("already been reported", await client.GetStringAsync(duplicate.Headers.Location!));
    }
    [Fact] public async Task InvalidPostPreservesEnteredValues()
    {
        using var factory = new TestApplicationFactory(); using var client = Client(factory);
        var (html, _) = await RatingForm(client); var fields = RatingFields(html); fields.Remove("Input.ProblemIds");
        using var response = await client.PostAsync("/CallList/Rate", new FormUrlEncodedContent(fields));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode); var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Select at least one problem", body); Assert.Contains("test details", body);
    }
    [Fact] public async Task RatingPostRequiresAntiforgeryToken()
    {
        using var factory = new TestApplicationFactory(); using var client = Client(factory);
        var (html, _) = await RatingForm(client); var fields = RatingFields(html); fields.Remove("__RequestVerificationToken");
        using var response = await client.PostAsync("/CallList/Rate", new FormUrlEncodedContent(fields)); Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    [Fact] public async Task AdminCanFilterPageUpdateAndExport()
    {
        using var factory = new TestApplicationFactory(); using var client = Client(factory);
        Assert.Contains("Page 2 of 3", await client.GetStringAsync("/Admin?page=2"));
        Assert.Contains("Page 1 of 1", await client.GetStringAsync("/Admin?statusId=2"));
        var edit = await client.GetStringAsync("/Admin/Edit/1");
        using var saved = await client.PostAsync("/Admin/Edit", new FormUrlEncodedContent(new Dictionary<string, string>
        { ["__RequestVerificationToken"] = Token(edit), ["Input.Id"] = "1", ["Input.StatusId"] = "2", ["Input.ReasonId"] = "1", ["Input.Note"] = "Resolved by test" }));
        Assert.Equal(HttpStatusCode.Redirect, saved.StatusCode); Assert.Contains("Resolved by test", await client.GetStringAsync("/Admin/Edit/1"));
        using var report = await client.GetAsync("/Admin/Export"); Assert.Equal(HttpStatusCode.OK, report.StatusCode);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", report.Content.Headers.ContentType!.MediaType);
        Assert.True((await report.Content.ReadAsByteArrayAsync()).Length > 1000);
    }
    [Fact] public async Task CallDownloadResolvesApiRecording()
    {
        using var factory = new TestApplicationFactory(); using var client = Client(factory);
        var calls = await client.GetStringAsync("/CallList");
        var url = WebUtility.HtmlDecode(Regex.Match(calls, "href=\"([^\"]*/CallList/Recording[^\"]*)\"").Groups[1].Value);
        using var response = await client.GetAsync(url); Assert.Equal("/test-recording.wav", response.Headers.Location!.ToString());
        using var wav = await client.GetAsync(response.Headers.Location); Assert.Equal("audio/wav", wav.Content.Headers.ContentType!.MediaType);
    }
    [Theory] [InlineData("/Admin/Edit/999", 404)] [InlineData("/Admin?statusId=999", 400)] [InlineData("/CallList?date=invalid", 400)]
    public async Task BadInputReturnsUsefulStatus(string url, int status)
    {
        using var factory = new TestApplicationFactory(); using var client = Client(factory); using var response = await client.GetAsync(url);
        Assert.Equal(status, (int)response.StatusCode); Assert.Contains("Reference:", await response.Content.ReadAsStringAsync());
    }
    [Theory] [InlineData(false, 503)] [InlineData(true, 500)]
    public async Task ErrorsAreSanitized(bool unexpected, int status)
    {
        var manager = A.Fake<ICallManager>();
        A.CallTo(() => manager.GetCallsAsync(A<Guid>._, A<DateOnly>._, A<CancellationToken>._)).ThrowsAsync(unexpected
            ? new InvalidOperationException("private-secret-connection-string") : new DependencyException("Calls service is unavailable."));
        using var factory = new TestApplicationFactory(s => { s.RemoveAll<ICallManager>(); s.AddSingleton(manager); }); using var client = Client(factory);
        using var response = await client.GetAsync("/CallList"); Assert.Equal(status, (int)response.StatusCode);
        Assert.DoesNotContain("private-secret", await response.Content.ReadAsStringAsync());
    }
    [Fact] public async Task ModalGetReturnsOnlyRatingForm()
    {
        using var factory = new TestApplicationFactory(); using var client = Client(factory); var (_, url) = await RatingForm(client);
        using var request = new HttpRequestMessage(HttpMethod.Get, url); request.Headers.Add("X-Rating-Modal", "true");
        using var response = await client.SendAsync(request); var html = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode); Assert.Contains("data-rating-form", html); Assert.DoesNotContain("<!DOCTYPE", html);
    }
    [Theory] [InlineData(true, 200, 13)] [InlineData(false, 422, 12)]
    public async Task ModalPostSavesOnlyValidRatings(bool valid, int status, int expectedRows)
    {
        using var factory = new TestApplicationFactory(); using var client = Client(factory); var (html, _) = await RatingForm(client);
        var fields = RatingFields(html); if (!valid) fields.Remove("Input.ProblemIds");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/CallList/Rate") { Content = new FormUrlEncodedContent(fields) };
        request.Headers.Add("X-Rating-Modal", "true"); using var response = await client.SendAsync(request);
        Assert.Equal(status, (int)response.StatusCode);
        if (valid)
        {
            using var json = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.True(json.RootElement.GetProperty("saved").GetBoolean()); Assert.True(json.RootElement.GetProperty("notificationSent").GetBoolean());
        }
        else { var body = await response.Content.ReadAsStringAsync(); Assert.Contains("test details", body); Assert.DoesNotContain("<!DOCTYPE", body); }
        using var scope = factory.Services.CreateScope(); Assert.Equal(expectedRows, (await scope.ServiceProvider.GetRequiredService<IRatingRepository>().GetRatingsAsync(null, default)).Count);
    }
}

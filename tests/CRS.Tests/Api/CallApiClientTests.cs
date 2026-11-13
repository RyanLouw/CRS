using System.Net;
using System.Text;
using System.Text.Json;
using CRS.Core.Api;
using CRS.Core.Models;
using FakeItEasy;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace CRS.Tests.Api;

public sealed class CallApiClientTests
{
    private readonly IAccessTokenProvider tokens = A.Fake<IAccessTokenProvider>();
    private readonly StubHandler handler = new();
    private readonly CallApiClient api;
    public CallApiClientTests()
    {
        A.CallTo(() => tokens.GetAsync(A<CancellationToken>._)).Returns("test-only-token");
        api = new(new HttpClient(handler) { BaseAddress = new Uri("https://api.example.invalid/") }, tokens,
            Options.Create(new CallApiOptions { RecordingHosts = ["recordings.example.invalid"] }), NullLogger<CallApiClient>.Instance);
    }
    private Task<IReadOnlyList<CallInfo>> Calls(CancellationToken ct = default) => api.GetCallsAsync("330", new(2026, 11, 13), ct);
    [Fact] public async Task PostsExpectedContractAndPerRequestBearerToken()
    {
        handler.Body = "[]";
        await Calls();
        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.Equal("/api/3cx/call-logs/by-extension", handler.Uri!.AbsolutePath);
        Assert.Equal("Bearer test-only-token", handler.Authorization);
        using var json = JsonDocument.Parse(handler.RequestBody!);
        Assert.Equal("330", json.RootElement.GetProperty("extension").GetString());
        Assert.Equal("Both", json.RootElement.GetProperty("callType").GetString());
        Assert.StartsWith("2026-11-13", json.RootElement.GetProperty("date").GetString());
    }
    [Theory] [InlineData("[]")] [InlineData("null")]
    public async Task EmptyResponseIsHandled(string json) { handler.Body = json; Assert.Empty(await Calls()); }
    [Fact] public async Task ParsesCallFields()
    {
        handler.Body = """[{"callID":"123","extension":"330","callDateTime":"2026-11-13T09:30:00","isAnswered":true,"recordingID":"file.wav"}]""";
        var call = Assert.Single(await Calls()); Assert.Equal("123", call.CallID); Assert.Equal("file.wav", call.RecordingKey);
    }
    [Theory] [InlineData(400)] [InlineData(401)] [InlineData(403)] [InlineData(404)] [InlineData(429)] [InlineData(500)] [InlineData(503)]
    public async Task HttpFailuresBecomeSafeDependencyErrors(int status)
    {
        handler.Status = (HttpStatusCode)status; handler.Body = "secret-upstream-error";
        var error = await Assert.ThrowsAsync<DependencyException>(() => Calls());
        Assert.DoesNotContain("secret-upstream-error", error.Message);
    }
    [Theory] [InlineData("not json")] [InlineData("{}")] [InlineData("[null]")] [InlineData("[{}]")] [InlineData("[{\"callDateTime\":\"bad-date\"}]")]
    public async Task MalformedOrIncompleteDataIsRejected(string json)
    {
        handler.Body = json; await Assert.ThrowsAsync<DependencyException>(() => Calls());
    }
    [Fact] public async Task NetworkFailureHasSafeError()
    {
        handler.Error = new HttpRequestException("secret endpoint");
        var error = await Assert.ThrowsAsync<DependencyException>(() => Calls()); Assert.DoesNotContain("secret", error.Message);
    }
    [Fact] public async Task TimeoutHasActionableMessage()
    {
        handler.Error = new TaskCanceledException();
        Assert.Contains("timed out", (await Assert.ThrowsAsync<DependencyException>(() => Calls())).Message);
    }
    [Fact] public async Task CallerCancellationIsNotChangedToTimeout()
    {
        using var cts = new CancellationTokenSource(); cts.Cancel();
        handler.Error = new OperationCanceledException(cts.Token);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Calls(cts.Token));
    }
    [Fact] public async Task MissingTokenStopsHttpRequest()
    {
        var provider = new ConfiguredAccessTokenProvider(Options.Create(new CallApiOptions()));
        await Assert.ThrowsAsync<DependencyException>(() => provider.GetAsync(default));
    }
    [Theory] [InlineData("https://recordings.example.invalid/file.wav")] [InlineData("https://RECORDINGS.example.invalid/file?signature=test")]
    public async Task AllowsOnlyConfiguredHttpsRecordingHosts(string url)
    {
        handler.Body = JsonSerializer.Serialize(new { url });
        Assert.StartsWith("https://recordings.example.invalid/", await api.GetRecordingAsync("file name.wav", default));
        Assert.Equal(HttpMethod.Get, handler.Method);
        Assert.Contains("file%20name.wav", handler.Uri!.AbsoluteUri);
    }
    [Theory] [InlineData("http://recordings.example.invalid/file")] [InlineData("https://attacker.invalid/file")]
    [InlineData("javascript:alert(1)")] [InlineData("//recordings.example.invalid/file")] [InlineData("https://user:pass@recordings.example.invalid/file")]
    public async Task RejectsUnsafeRecordingUrls(string url)
    {
        handler.Body = JsonSerializer.Serialize(new { url });
        await Assert.ThrowsAsync<DependencyException>(() => api.GetRecordingAsync("recording", default));
    }
    [Fact] public async Task MissingRecordingUrlReturnsNull()
    {
        handler.Body = "{\"url\":null}"; Assert.Null(await api.GetRecordingAsync("file", default));
    }
    private sealed class StubHandler : HttpMessageHandler
    {
        public string Body { get; set; } = "[]";
        public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;
        public Exception? Error { get; set; }
        public Uri? Uri { get; private set; }
        public HttpMethod? Method { get; private set; }
        public string? Authorization { get; private set; }
        public string? RequestBody { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (Error != null) throw Error;
            Uri = request.RequestUri; Method = request.Method; Authorization = request.Headers.Authorization?.ToString();
            RequestBody = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return new(Status) { Content = new StringContent(Body, Encoding.UTF8, "application/json") };
        }
    }
}

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CRS.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CRS.Core.Api;

public sealed class CallApiOptions
{
    public string BaseUrl { get; set; } = "";
    public string? AccessToken { get; set; }
    public string[] RecordingHosts { get; set; } = [];
    public int TimeoutSeconds { get; set; } = 30;
}
public interface IAccessTokenProvider
{
    Task<string> GetAsync(CancellationToken cancellationToken);
}
// Replace via DI with your token service when using the private central-config authentication flow.
public sealed class ConfiguredAccessTokenProvider(IOptions<CallApiOptions> options) : IAccessTokenProvider
{
    public Task<string> GetAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var token = options.Value.AccessToken;
        if (string.IsNullOrWhiteSpace(token)) throw new DependencyException("The calls API token is not configured. Contact IT.");
        return Task.FromResult(token);
    }
}

public sealed class CallApiClient(HttpClient client, IAccessTokenProvider tokens, IOptions<CallApiOptions> options,
    ILogger<CallApiClient> logger) : ICallApi
{
    public async Task<IReadOnlyList<CallInfo>> GetCallsAsync(string extension, DateOnly date, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(extension)) throw new WorkflowException("A telephone extension is required.");
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/3cx/call-logs/by-extension")
        {
            Content = JsonContent.Create(new { extension, callType = "Both", date = date.ToDateTime(TimeOnly.MinValue) })
        };
        var calls = await SendAsync<List<CallInfo>>(request, ct) ?? [];
        if (calls.Any(c => c is null || string.IsNullOrWhiteSpace(c.CallID) || string.IsNullOrWhiteSpace(c.Extension) || !c.CallDateTime.HasValue))
            throw new DependencyException("The calls service returned incomplete call details. Contact IT.");
        return calls;
    }
    public async Task<string?> GetRecordingAsync(string key, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new MissingRecordException("No recording is available.");
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/3cx/call-logs/call-url/" + Uri.EscapeDataString(key));
        var result = await SendAsync<RecordingResponse>(request, ct);
        if (string.IsNullOrWhiteSpace(result?.Url)) return null;
        if (!Uri.TryCreate(result.Url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps ||
            !string.IsNullOrEmpty(uri.UserInfo) || !options.Value.RecordingHosts.Contains(uri.Host, StringComparer.OrdinalIgnoreCase))
        {
            logger.LogWarning("API returned a recording URL outside the configured HTTPS hosts");
            throw new DependencyException("The recording service returned an invalid download address. Contact IT.");
        }
        return uri.AbsoluteUri;
    }
    private async Task<T?> SendAsync<T>(HttpRequestMessage request, CancellationToken ct)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await tokens.GetAsync(ct));
        try
        {
            using var response = await client.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Calls API request failed with status {StatusCode}", (int)response.StatusCode);
                throw new DependencyException("The calls service is unavailable or rejected access. Please try again or contact IT.");
            }
            var result = await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
            logger.LogInformation("Calls API request completed");
            return result;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning("Calls API request timed out");
            throw new DependencyException("The calls service timed out. Please try again.");
        }
        catch (HttpRequestException)
        {
            logger.LogWarning("Calls API connection failed");
            throw new DependencyException("The calls service could not be reached. Please try again.");
        }
        catch (JsonException)
        {
            logger.LogWarning("Calls API returned invalid JSON");
            throw new DependencyException("The calls service returned an invalid response. Contact IT.");
        }
    }
    private sealed record RecordingResponse(string? Url);
}

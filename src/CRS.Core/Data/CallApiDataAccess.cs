using System.Net.Http.Json;
using System.Text.Json;
using CRS.Core.Models;
using Serilog;

namespace CRS.Core.Data;

public class CallApiDataAccess(HttpClient client)
{
    public virtual async Task<IReadOnlyList<CallInfo>> GetCallsAsync(string extension, DateOnly date, CancellationToken ct)
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
    public virtual async Task<string?> GetRecordingAsync(string key, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new MissingRecordException("No recording is available.");
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/3cx/call-logs/call-url/" + Uri.EscapeDataString(key));
        var result = await SendAsync<JsonElement>(request, ct);
        if (result.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined) return null;
        if (result.ValueKind != JsonValueKind.Object)
            throw new DependencyException("The recording service returned an invalid response.");
        if (!result.TryGetProperty("url", out var value) && !result.TryGetProperty("Url", out value)) return null;
        if (value.ValueKind == JsonValueKind.Null) return null;
        if (value.ValueKind != JsonValueKind.String)
            throw new DependencyException("The recording service returned an invalid response.");
        var url = value.GetString();
        if (string.IsNullOrWhiteSpace(url)) return null;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(uri.UserInfo))
        {
            Log.Warning("API returned a recording URL with an invalid HTTPS address");
            throw new DependencyException("The recording service returned an invalid download address. Contact IT.");
        }
        return uri.AbsoluteUri;
    }
    private async Task<T?> SendAsync<T>(HttpRequestMessage request, CancellationToken ct)
    {
        try
        {
            using var response = await client.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                Log.Warning("Calls API request failed with status {StatusCode}", (int)response.StatusCode);
                throw new DependencyException("The calls service is unavailable or rejected access. Please try again or contact IT.");
            }
            var result = await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
            Log.Information("Calls API request completed");
            return result;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            Log.Warning("Calls API request timed out");
            throw new DependencyException("The calls service timed out. Please try again.");
        }
        catch (HttpRequestException)
        {
            Log.Warning("Calls API connection failed");
            throw new DependencyException("The calls service could not be reached. Please try again.");
        }
        catch (JsonException)
        {
            Log.Warning("Calls API returned invalid JSON");
            throw new DependencyException("The calls service returned an invalid response. Contact IT.");
        }
    }
}

using CRS.Tests.Fakes;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CRS.Tests;

public sealed class WebTests
{
    [Theory] [InlineData("/CallList", "My calls")] [InlineData("/Admin", "Help desk")]
    public async Task PagesRender(string path, string expected)
    {
        using var factory = new TestApplicationFactory(); using var client = factory.CreateClient();
        using var response = await client.GetAsync(path); response.EnsureSuccessStatusCode();
        Assert.Contains(expected, await response.Content.ReadAsStringAsync());
    }
    [Fact] public async Task ControllerRequestLogOmitsSensitiveQueryAndRawPath()
    {
        var directory = Path.Combine(Path.GetTempPath(), "crs-private-test-" + Guid.NewGuid());
        try
        {
            using (var factory = new TestApplicationFactory(logDirectory: directory))
            using (var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") }))
            using (var response = await client.GetAsync("/CallList?token=private-test-value")) response.EnsureSuccessStatusCode();
            var logs = string.Join("", Directory.GetFiles(directory, "*.log").Select(File.ReadAllText));
            Assert.Contains("Controller CallList.Index completed 200", logs);
            Assert.DoesNotContain("private-test-value", logs); Assert.DoesNotContain("/CallList", logs);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
}

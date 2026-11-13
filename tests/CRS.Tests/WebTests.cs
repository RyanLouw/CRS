using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Xunit;

namespace CRS.Tests;

public sealed class WebTests
{
    [Theory]
    [InlineData("/", "Welcome")]
    [InlineData("/Home/Privacy", "Privacy Policy")]
    public async Task PagesRender(string path, string expected)
    {
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.UseEnvironment("Development").UseSetting("Logging:Directory", Path.Combine(Path.GetTempPath(), "crs-web-tests")));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        using var response = await client.GetAsync(path);
        response.EnsureSuccessStatusCode();
        Assert.Contains(expected, await response.Content.ReadAsStringAsync());
    }
    [Fact]
    public async Task MissingRouteReturns404WithoutLoggingSensitiveQuery()
    {
        var directory = Path.Combine(Path.GetTempPath(), "crs-private-test-" + Guid.NewGuid());
        try
        {
            using (var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
                builder.UseEnvironment("Development").UseSetting("Logging:Directory", directory)))
            using (var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") }))
            using (var response = await client.GetAsync("/missing-route?token=private-test-value"))
                Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);

            var logs = string.Join("", Directory.GetFiles(directory, "*.log").Select(File.ReadAllText));
            Assert.Contains("404", logs);
            Assert.DoesNotContain("private-test-value", logs);
            Assert.DoesNotContain("missing-route", logs);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }
}

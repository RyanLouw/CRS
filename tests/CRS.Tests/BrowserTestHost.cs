using CRS.Tests.Fakes;

namespace CRS.Tests;

// Optional test-only HTTP host for browser/manual walkthroughs. Never referenced by Core or Web.
public static class BrowserTestHost
{
    public static async Task Main(string[] args)
    {
        var port = args.Length > 0 ? int.Parse(args[0]) : 5174;
        using var factory = new TestApplicationFactory();
        factory.UseKestrel(port);
        using var client = factory.CreateClient();
        Console.WriteLine($"TEST FIXTURES ONLY: local browser host started on port {port}. No real API, database or email is used.");
        await Task.Delay(Timeout.InfiniteTimeSpan);
    }
}

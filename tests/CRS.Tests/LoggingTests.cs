using CRS.Logging;
using FakeItEasy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Xunit;

namespace CRS.Tests;

public sealed class LoggingTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "crs-tests-" + Guid.NewGuid());
    private readonly IHostEnvironment environment = A.Fake<IHostEnvironment>();

    public LoggingTests()
    {
        Directory.CreateDirectory(root);
        A.CallTo(() => environment.ContentRootPath).Returns(root);
    }

    private static IConfiguration Config(string? directory = null) => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["Logging:Directory"] = directory }).Build();

    [Fact]
    public void UsesConfiguredAbsoluteDirectory() =>
        Assert.Equal(root, ApplicationLogging.GetDirectory(Config(root), environment));

    [Fact]
    public void DefaultsToPlatformDirectory() => Assert.Equal(
        OperatingSystem.IsWindows() ? @"C:\CallRatingSystem\Logs" : Path.Combine(root, "Logs"),
        ApplicationLogging.GetDirectory(Config(), environment));

    [Theory]
    [InlineData("relative/logs")]
    [InlineData("..")]
    public void RejectsRelativeDirectory(string directory) => Assert.Throws<InvalidOperationException>(
        () => ApplicationLogging.GetDirectory(Config(directory), environment));

    [Fact]
    public void FailsClearlyWhenDirectoryIsAFile()
    {
        var file = Path.Combine(root, "file");
        File.WriteAllText(file, "occupied");
        Assert.Throws<IOException>(() => ApplicationLogging.Configure(new LoggerConfiguration(), Config(file), environment));
    }

    [Fact]
    public void WritesStructuredEventsAndRollsAwayFilesOlderThanSevenDays()
    {
        var stale = Path.Combine(root, "crs-" + DateTime.Now.AddDays(-9).ToString("yyyyMMdd") + ".log");
        var recent = Path.Combine(root, "crs-" + DateTime.Now.AddDays(-2).ToString("yyyyMMdd") + ".log");
        File.WriteAllText(stale, "old");
        File.WriteAllText(recent, "recent");
        using (var logger = ApplicationLogging.Configure(new LoggerConfiguration(), Config(root), environment).CreateLogger())
            logger.Information("Rating saved {RatingId}", 123);
        Assert.False(File.Exists(stale));
        Assert.True(File.Exists(recent));
        Assert.Contains(Directory.GetFiles(root, "crs-*.log"), file => File.ReadAllText(file).Contains("Rating saved 123"));
    }

    public void Dispose() => Directory.Delete(root, true);
}

using Serilog;
using Serilog.Events;

namespace CRS.Logging;

public static class ApplicationLogging
{
    public static string GetDirectory(IConfiguration configuration, IHostEnvironment environment)
    {
        var configured = configuration["Logging:Directory"];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            if (!Path.IsPathFullyQualified(configured))
                throw new InvalidOperationException("Logging:Directory must be an absolute path.");
            return configured;
        }

        return OperatingSystem.IsWindows()
            ? @"C:\CallRatingSystem\Logs"
            : Path.Combine(environment.ContentRootPath, "Logs");
    }

    public static LoggerConfiguration Configure(
        LoggerConfiguration logger, IConfiguration configuration, IHostEnvironment environment)
    {
        var directory = GetDirectory(configuration, environment);
        Directory.CreateDirectory(directory);
        return logger.MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            // HTTP client default diagnostics can contain URLs with sensitive query parameters.
            .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.With<RequestMetadataFilter>()
            .WriteTo.Console()
            .WriteTo.File(Path.Combine(directory, "crs-.log"),
                rollingInterval: RollingInterval.Day,
                rollOnFileSizeLimit: true,
                fileSizeLimitBytes: 100 * 1024 * 1024,
                retainedFileCountLimit: null,
                retainedFileTimeLimit: TimeSpan.FromDays(7),
                outputTemplate: "{Timestamp:O} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");
    }
}

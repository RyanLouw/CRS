using CRS.Core.Data;
using CRS.Core.Models;
using FakeItEasy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;

namespace CRS.Tests.Fakes;

public sealed class TestApplicationFactory(Action<IServiceCollection>? configure = null, string? logDirectory = null) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseContentRoot(FindWebRoot()).UseEnvironment("Development")
            .UseSetting("CurrentUser:Id", DemoServices.OperatorId.ToString())
            .UseSetting("Logging:Directory", logDirectory ?? Path.Combine(Path.GetTempPath(), "crs-test-host-logs"));
        builder.ConfigureServices(services =>
        {
            services.AddSingleton(new DemoServices(TimeProvider.System, NullLogger<DemoServices>.Instance));
            services.RemoveAll<IRatingRepository>(); services.AddSingleton<IRatingRepository>(s => s.GetRequiredService<DemoServices>());
            services.RemoveAll<IRatingNotifier>(); services.AddSingleton<IRatingNotifier>(s => s.GetRequiredService<DemoServices>());
            services.RemoveAll<CallApiDataAccess>();
            services.AddSingleton(s =>
            {
                var demo = s.GetRequiredService<DemoServices>();
                var api = A.Fake<CallApiDataAccess>(o => o.WithArgumentsForConstructor(new object[] { new HttpClient() }));
                A.CallTo(() => api.GetCallsAsync(A<string>._, A<DateOnly>._, A<CancellationToken>._))
                    .ReturnsLazily((string extension, DateOnly date, CancellationToken ct) => demo.GetCallsAsync(extension, date, ct));
                A.CallTo(() => api.GetRecordingAsync(A<string>._, A<CancellationToken>._))
                    .ReturnsLazily((string key, CancellationToken ct) => demo.GetRecordingAsync(key, ct));
                return api;
            });
            services.AddSingleton<IStartupFilter, TestHostFilter>();
            configure?.Invoke(services);
        });
    }
    private static string FindWebRoot()
    {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "src", "CRS.Web");
            if (File.Exists(Path.Combine(candidate, "CRS.Web.csproj"))) return candidate;
        }
        throw new InvalidOperationException("Run the test host from a checkout containing src/CRS.Web.");
    }
    private sealed class TestHostFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use(async (context, continuation) =>
            {
                context.Response.Headers["X-CRS-Test-Host"] = "true";
                if (context.Request.Path == "/test-recording.wav")
                {
                    context.Response.ContentType = "audio/wav";
                    context.Response.Headers.ContentDisposition = "attachment; filename=test-recording.wav";
                    using var stream = new MemoryStream();
                    using var writer = new BinaryWriter(stream);
                    writer.Write("RIFF"u8); writer.Write(36 + 16000); writer.Write("WAVEfmt "u8);
                    writer.Write(16); writer.Write((short)1); writer.Write((short)1); writer.Write(8000);
                    writer.Write(16000); writer.Write((short)2); writer.Write((short)16);
                    writer.Write("data"u8); writer.Write(16000); writer.Write(new byte[16000]);
                    await context.Response.Body.WriteAsync(stream.ToArray());
                    return;
                }
                await continuation();
            });
            next(app);
        };
    }
}

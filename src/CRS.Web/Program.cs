using CRS.Logging;
using Serilog;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();
try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Services.AddSerilog((services, logger) =>
        ApplicationLogging.Configure(logger, builder.Configuration, builder.Environment));
    builder.Services.AddControllersWithViews();

    var app = builder.Build();
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }

    app.Use(async (context, next) =>
    {
        var started = System.Diagnostics.Stopwatch.GetTimestamp();
        try
        {
            await next(context);
            Log.Information("HTTP completed {StatusCode} in {ElapsedMs} ms; trace {TraceId}",
                context.Response.StatusCode,
                System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds,
                context.TraceIdentifier);
        }
        catch (Exception error)
        {
            Log.Error("HTTP failed with {ErrorType}; trace {TraceId}",
                error.GetType().Name, context.TraceIdentifier);
            throw;
        }
    });
    app.UseHttpsRedirection();
    app.UseRouting();
    app.UseAuthorization();
    app.MapStaticAssets();
    app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}")
        .WithStaticAssets();
    Log.Information("CallRatingSystem starting");
    app.Run();
}
catch (Exception exception)
{
    Log.Fatal(exception, "CallRatingSystem terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program;

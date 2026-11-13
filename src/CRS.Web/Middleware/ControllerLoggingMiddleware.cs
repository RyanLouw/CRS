using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Controllers;
using Serilog;

namespace CRS.Middleware;

// Runs after routing: static files and unmatched URLs are not controller requests.
public sealed class ControllerLoggingMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var action = context.GetEndpoint()?.Metadata.GetMetadata<ControllerActionDescriptor>();
        if (action is null) { await next(context); return; }
        var started = Stopwatch.GetTimestamp();
        try
        {
            await next(context);
            Log.Information("Controller {Controller}.{Action} completed {StatusCode} in {ElapsedMs} ms; trace {TraceId}",
                action.ControllerName, action.ActionName, context.Response.StatusCode,
                Stopwatch.GetElapsedTime(started).TotalMilliseconds, context.TraceIdentifier);
        }
        catch (Exception error)
        {
            Log.Error("Controller {Controller}.{Action} failed with {ErrorType}; trace {TraceId}",
                action.ControllerName, action.ActionName, error.GetType().Name, context.TraceIdentifier);
            throw;
        }
    }
}

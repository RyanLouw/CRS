using Serilog.Core;
using Serilog.Events;

namespace CRS.Logging;

// ASP.NET adds request paths through logging scopes even when our message omits them.
public sealed class RequestMetadataFilter : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        foreach (var name in new[] { "RequestPath", "QueryString", "RequestHeaders", "RequestBody", "ResponseBody" })
            logEvent.RemovePropertyIfPresent(name);
    }
}

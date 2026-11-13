using CRS.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Xunit;

namespace CRS.Tests;

public sealed class ControllerLoggingTests : IDisposable
{
    private readonly Serilog.ILogger previous = Log.Logger;
    private readonly EventSink sink = new();
    public ControllerLoggingTests() => Log.Logger = new LoggerConfiguration().WriteTo.Sink(sink).CreateLogger();
    [Theory] [InlineData("/css/site.css")] [InlineData("/missing")]
    public async Task IgnoresRequestsThatAreNotControllerActions(string path)
    {
        var context = new DefaultHttpContext(); context.Request.Path = path; var called = false;
        await new ControllerLoggingMiddleware(_ => { called = true; return Task.CompletedTask; }).InvokeAsync(context);
        Assert.True(called); Assert.Empty(sink.Events);
    }
    [Fact] public async Task LogsControllerNameActionStatusAndTimingWithoutQueryValues()
    {
        var context = ControllerContext(); context.Request.QueryString = new("?token=private-value");
        await new ControllerLoggingMiddleware(_ => { context.Response.StatusCode = 201; return Task.CompletedTask; }).InvokeAsync(context);
        var entry = Assert.Single(sink.Events); Assert.Equal("CallList", ((ScalarValue)entry.Properties["Controller"]).Value); Assert.Equal("Index", ((ScalarValue)entry.Properties["Action"]).Value); Assert.Contains("201", entry.RenderMessage());
        Assert.DoesNotContain("private-value", entry.RenderMessage());
    }
    [Fact] public async Task LogsAndRethrowsControllerFailureWithoutExceptionDetails()
    {
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new ControllerLoggingMiddleware(_ => throw new InvalidOperationException("private-value")).InvokeAsync(ControllerContext()));
        Assert.Equal("private-value", error.Message); var entry = Assert.Single(sink.Events);
        Assert.Equal(LogEventLevel.Error, entry.Level); Assert.DoesNotContain("private-value", entry.RenderMessage());
    }
    private static DefaultHttpContext ControllerContext()
    {
        var context = new DefaultHttpContext();
        context.SetEndpoint(new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(new ControllerActionDescriptor
            { ControllerName = "CallList", ActionName = "Index" }), "test"));
        return context;
    }
    public void Dispose() { (Log.Logger as IDisposable)?.Dispose(); Log.Logger = previous; }
    private sealed class EventSink : ILogEventSink
    {
        public List<LogEvent> Events { get; } = [];
        public void Emit(LogEvent logEvent) => Events.Add(logEvent);
    }
}

using CRS.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Serilog;

namespace CRS.Middleware;

// Translate business/dependency errors into MVC responses; this is not authentication code.
public sealed class ControllerExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is OperationCanceledException && context.HttpContext.RequestAborted.IsCancellationRequested) return;
        var (status, message) = context.Exception switch
        {
            WorkflowException ex => (400, ex.Message),
            MissingRecordException ex => (404, ex.Message),
            DependencyException ex => (503, ex.Message),
            _ => (500, "The operation could not be completed. Please contact IT with the reference below.")
        };
        Log.Warning("Controller operation failed with {ErrorType}; trace {TraceId}", context.Exception.GetType().Name, context.HttpContext.TraceIdentifier);
        context.Result = new ViewResult { ViewName = "WorkflowError", StatusCode = status,
            ViewData = new Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary<string>(
                new Microsoft.AspNetCore.Mvc.ModelBinding.EmptyModelMetadataProvider(), context.ModelState) { Model = message } };
        context.ExceptionHandled = true;
    }
}

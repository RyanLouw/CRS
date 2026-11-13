using CRS.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CRS.Security;

public sealed class WorkflowExceptionFilter(ILogger<WorkflowExceptionFilter> logger) : IExceptionFilter
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
        logger.LogWarning("Workflow failed with {ErrorType}; status {StatusCode}; trace {TraceId}",
            context.Exception.GetType().Name, status, context.HttpContext.TraceIdentifier);
        var result = new ViewResult { ViewName = "WorkflowError", StatusCode = status,
            ViewData = new Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary<string>(
                new Microsoft.AspNetCore.Mvc.ModelBinding.EmptyModelMetadataProvider(), context.ModelState) { Model = message } };
        context.Result = result;
        context.ExceptionHandled = true;
    }
}

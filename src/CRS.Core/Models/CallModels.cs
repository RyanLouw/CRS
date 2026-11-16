namespace CRS.Core.Models;

public sealed class WorkflowException(string message) : Exception(message);
public sealed class MissingRecordException(string message) : Exception(message);
public sealed class DependencyException(string message) : Exception(message);

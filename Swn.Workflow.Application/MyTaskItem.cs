namespace Swn.Workflow.Application;

public sealed record MyTaskItem(
    Guid Id,
    Guid WorkflowInstanceId,
    string WorkflowName,
    string Subject,
    DateOnly ReferenceDate,
    string Key,
    string Title,
    string Description,
    string Phase,
    int SortOrder,
    string AssignedRoleKey,
    int Status,
    string? Comment);
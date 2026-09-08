namespace Swn.Workflow.Application;

public sealed record WorkflowTaskDetailItem(
    Guid Id,
    string Key,
    string Title,
    string Description,
    string Phase,
    int SortOrder,
    string AssignedRoleKey,
    bool IsOptional,
    int Status,
    string? AssignedUserId,
    string? Comment,
    string? CompletedByUserId,
    DateTime? CompletedAt);
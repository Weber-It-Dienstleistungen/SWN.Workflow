namespace Swn.Workflow.Application;

public sealed record WorkflowDetailItem(
    Guid Id,
    string WorkflowName,
    string Subject,
    DateOnly ReferenceDate,
    int Status,
    DateTime CreatedAt,
    string CreatedByUserId,
    DateTime? CompletedAt,
    IReadOnlyDictionary<string, string> Properties,
    IReadOnlyList<WorkflowTaskDetailItem> Tasks);
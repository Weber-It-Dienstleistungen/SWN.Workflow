namespace Swn.Workflow.Application;

public sealed record WorkflowOverviewItem(
    Guid Id,
    string WorkflowName,
    string Subject,
    DateOnly ReferenceDate,
    int Status,
    int TotalTasks,
    int CompletedTasks);
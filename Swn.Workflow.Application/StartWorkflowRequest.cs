namespace Swn.Workflow.Application;

public sealed record StartWorkflowRequest(
    string WorkflowKey,
    string Subject,
    DateOnly ReferenceDate,
    string CreatedByUserId,
    IReadOnlyDictionary<string, string>? Properties = null);
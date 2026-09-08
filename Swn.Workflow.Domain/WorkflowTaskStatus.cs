namespace Swn.Workflow.Domain;

public enum WorkflowTaskStatus
{
    Open = 0,
    InProgress = 1,
    Completed = 2,
    Blocked = 3,
    NotRequired = 4
}
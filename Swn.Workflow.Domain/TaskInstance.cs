namespace Swn.Workflow.Domain;

public class TaskInstance
{
    public Guid Id { get; set; }

    public Guid WorkflowInstanceId { get; set; }

    public Guid TaskDefinitionId { get; set; }

    public WorkflowTaskStatus Status { get; set; } = WorkflowTaskStatus.Open;

    public string AssignedRoleKey { get; set; } = string.Empty;

    public string? AssignedUserId { get; set; }

    public string? Comment { get; set; }

    public string? CompletedByUserId { get; set; }

    public DateTime? CompletedAt { get; set; }
}
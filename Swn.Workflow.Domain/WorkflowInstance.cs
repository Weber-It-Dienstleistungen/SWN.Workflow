namespace Swn.Workflow.Domain;

public class WorkflowInstance
{
    public Guid Id { get; set; }

    public Guid WorkflowVersionId { get; set; }

    public string Subject { get; set; } = string.Empty;

    public DateOnly ReferenceDate { get; set; }

    public WorkflowStatus Status { get; set; } = WorkflowStatus.Open;

    public DateTime CreatedAt { get; set; }

    public string CreatedByUserId { get; set; } = string.Empty;

    public DateTime? CompletedAt { get; set; }
}
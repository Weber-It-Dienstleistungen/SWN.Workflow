namespace Swn.Workflow.Domain;

public class WorkflowNotification
{
    public Guid Id { get; set; }

    public Guid WorkflowInstanceId { get; set; }

    public Guid? TaskInstanceId { get; set; }

    public string RecipientUserId { get; set; } =
        string.Empty;

    public string RecipientEmail { get; set; } =
        string.Empty;

    public WorkflowNotificationType Type { get; set; }

    public string TriggerKey { get; set; } =
        string.Empty;

    public WorkflowNotificationStatus Status { get; set; } =
        WorkflowNotificationStatus.Pending;

    public DateTime CreatedAt { get; set; }

    public int AttemptCount { get; set; }

    public DateTime? LastAttemptAt { get; set; }

    public DateTime? SentAt { get; set; }

    public string? LastError { get; set; }
}
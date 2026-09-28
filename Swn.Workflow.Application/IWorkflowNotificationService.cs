namespace Swn.Workflow.Application;

public interface IWorkflowNotificationService
{
    Task<WorkflowNotificationQueueResult>
        QueueTaskAvailableAsync(
            Guid taskInstanceId,
            CancellationToken cancellationToken = default);

    Task<WorkflowNotificationQueueResult>
        QueueWorkflowCompletedAsync(
            Guid workflowInstanceId,
            CancellationToken cancellationToken = default);
}

public sealed record WorkflowNotificationQueueResult(
    int CreatedCount,
    int AlreadyQueuedCount,
    int SkippedRecipientCount);
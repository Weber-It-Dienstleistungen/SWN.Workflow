namespace Swn.Workflow.Application;

public interface IWorkflowNotificationDispatcher
{
    Task<WorkflowNotificationDispatchResult>
        DispatchPendingTaskAvailableAsync(
            CancellationToken cancellationToken = default);

    Task<WorkflowNotificationDispatchResult>
        DispatchPendingWorkflowCompletedAsync(
            CancellationToken cancellationToken = default);
}

public sealed record WorkflowNotificationDispatchResult(
    int ProcessedGroupCount,
    int SentGroupCount,
    int FailedGroupCount,
    int SentNotificationCount,
    int FailedNotificationCount);
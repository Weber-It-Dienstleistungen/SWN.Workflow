namespace Swn.Workflow.Application;

public interface IWorkflowDetailService
{
    Task<WorkflowDetailItem?> GetForInitiatorAsync(
        Guid workflowInstanceId,
        string userId,
        CancellationToken cancellationToken = default);
}
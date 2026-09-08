namespace Swn.Workflow.Application;

public interface IWorkflowDetailService
{
    Task<WorkflowDetailItem?> GetByIdAsync(
        Guid workflowInstanceId,
        CancellationToken cancellationToken = default);
}
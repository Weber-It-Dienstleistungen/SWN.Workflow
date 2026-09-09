namespace Swn.Workflow.Application;

public interface IWorkflowDetailService
{
    Task<WorkflowDetailItem?> GetByIdAsync(
        Guid workflowInstanceId,
        CancellationToken cancellationToken = default);

    Task<WorkflowDetailItem?> GetForUserAsync(
        Guid workflowInstanceId,
        string userId,
        IReadOnlyCollection<string> roleKeys,
        bool includeAllTasks,
        CancellationToken cancellationToken = default);
}
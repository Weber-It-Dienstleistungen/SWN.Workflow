namespace Swn.Workflow.Application;

public interface IDashboardService
{
    Task<IReadOnlyList<WorkflowOverviewItem>>
        GetActiveWorkflowsForUserAsync(
            string userId,
            CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AvailableWorkflowItem>>
        GetAvailableWorkflowsAsync(
            CancellationToken cancellationToken = default);
}
namespace Swn.Workflow.Application;

public interface IDashboardService
{
    Task<IReadOnlyList<WorkflowOverviewItem>> GetActiveWorkflowsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AvailableWorkflowItem>> GetAvailableWorkflowsAsync(
        CancellationToken cancellationToken = default);
}
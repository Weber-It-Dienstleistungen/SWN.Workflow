namespace Swn.Workflow.Application;

public interface IDashboardService
{
    Task<IReadOnlyList<WorkflowOverviewItem>> GetActiveWorkflowsAsync(
        CancellationToken cancellationToken = default);
}
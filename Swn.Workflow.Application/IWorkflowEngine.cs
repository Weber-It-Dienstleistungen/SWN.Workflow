namespace Swn.Workflow.Application;

public interface IWorkflowEngine
{
    Task<Guid> StartAsync(
        StartWorkflowRequest request,
        CancellationToken cancellationToken = default);
}
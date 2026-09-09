namespace Swn.Workflow.Application;

public interface IWorkflowTaskService
{
    Task UpdateStatusAsync(
        Guid taskInstanceId,
        int status,
        string changedByUserId,
        CancellationToken cancellationToken = default);

    Task UpdateCommentAsync(
        Guid taskInstanceId,
        string? comment,
        CancellationToken cancellationToken = default);
}
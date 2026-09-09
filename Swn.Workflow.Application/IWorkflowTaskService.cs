namespace Swn.Workflow.Application;

public interface IWorkflowTaskService
{
    Task UpdateStatusAsync(
        Guid taskInstanceId,
        int status,
        string changedByUserId,
        IReadOnlyCollection<string> roleKeys,
        CancellationToken cancellationToken = default);

    Task UpdateCommentAsync(
        Guid taskInstanceId,
        string? comment,
        string changedByUserId,
        IReadOnlyCollection<string> roleKeys,
        CancellationToken cancellationToken = default);
}
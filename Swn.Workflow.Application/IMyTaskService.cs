namespace Swn.Workflow.Application;

public interface IMyTaskService
{
    Task<IReadOnlyList<MyTaskItem>> GetOpenTasksAsync(
        string userId,
        IReadOnlyCollection<string> roleKeys,
        CancellationToken cancellationToken = default);
}
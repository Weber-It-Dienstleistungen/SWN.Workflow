using Microsoft.EntityFrameworkCore;
using Swn.Workflow.Application;
using Swn.Workflow.Domain;

namespace Swn.Workflow.Infrastructure;

public sealed class MyTaskService : IMyTaskService
{
    private readonly IDbContextFactory<WorkflowDbContext> _dbContextFactory;

    public MyTaskService(
        IDbContextFactory<WorkflowDbContext> dbContextFactory)
    {
        ArgumentNullException.ThrowIfNull(dbContextFactory);

        _dbContextFactory = dbContextFactory;
    }

    public async Task<IReadOnlyList<MyTaskItem>> GetOpenTasksAsync(
        string userId,
        IReadOnlyCollection<string> roleKeys,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentNullException.ThrowIfNull(roleKeys);

        var normalizedUserId = userId.Trim();

        if (string.IsNullOrWhiteSpace(normalizedUserId))
        {
            throw new ArgumentException(
                "User ID must not be empty.",
                nameof(userId));
        }

        var normalizedRoleKeys = roleKeys
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Select(role => role.Trim().ToUpperInvariant())
            .Distinct()
            .ToArray();

        await using var db =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        return await (
            from taskInstance in db.TaskInstances.AsNoTracking()

            join taskDefinition in db.TaskDefinitions.AsNoTracking()
                on taskInstance.TaskDefinitionId equals taskDefinition.Id

            join workflowInstance in db.WorkflowInstances.AsNoTracking()
                on taskInstance.WorkflowInstanceId equals workflowInstance.Id

            join workflowVersion in db.WorkflowVersions.AsNoTracking()
                on workflowInstance.WorkflowVersionId equals workflowVersion.Id

            join workflowDefinition in db.WorkflowDefinitions.AsNoTracking()
                on workflowVersion.WorkflowDefinitionId equals workflowDefinition.Id

            where
                (workflowInstance.Status == WorkflowStatus.Open ||
                 workflowInstance.Status == WorkflowStatus.InProgress)

                && taskInstance.Status != WorkflowTaskStatus.Completed

                && taskInstance.Status != WorkflowTaskStatus.NotRequired

                && (
                    taskInstance.AssignedUserId == normalizedUserId
                    ||
                    (
                        taskInstance.AssignedUserId == null
                        && normalizedRoleKeys.Contains(
                            taskInstance.AssignedRoleKey)
                    )
                )

            orderby
                workflowInstance.ReferenceDate,
                workflowDefinition.Name,
                workflowInstance.Subject,
                taskDefinition.SortOrder

            select new MyTaskItem(
                taskInstance.Id,
                workflowInstance.Id,
                workflowDefinition.Name,
                workflowInstance.Subject,
                workflowInstance.ReferenceDate,
                taskDefinition.Key,
                taskDefinition.Title,
                taskDefinition.Description,
                taskDefinition.Phase,
                taskDefinition.SortOrder,
                taskInstance.AssignedRoleKey,
                (int)taskInstance.Status,
                taskInstance.Comment)
        )
        .ToListAsync(cancellationToken);
    }
}
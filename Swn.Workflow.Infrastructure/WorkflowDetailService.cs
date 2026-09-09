using Microsoft.EntityFrameworkCore;
using Swn.Workflow.Application;
using Swn.Workflow.Domain;

namespace Swn.Workflow.Infrastructure;

public sealed class WorkflowDetailService : IWorkflowDetailService
{
    private readonly IDbContextFactory<WorkflowDbContext> _dbContextFactory;

    public WorkflowDetailService(
        IDbContextFactory<WorkflowDbContext> dbContextFactory)
    {
        ArgumentNullException.ThrowIfNull(dbContextFactory);

        _dbContextFactory = dbContextFactory;
    }

    public Task<WorkflowDetailItem?> GetByIdAsync(
        Guid workflowInstanceId,
        CancellationToken cancellationToken = default)
    {
        return GetInternalAsync(
            workflowInstanceId,
            userId: null,
            roleKeys: Array.Empty<string>(),
            includeAllTasks: true,
            cancellationToken);
    }

    public Task<WorkflowDetailItem?> GetForUserAsync(
        Guid workflowInstanceId,
        string userId,
        IReadOnlyCollection<string> roleKeys,
        bool includeAllTasks,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentNullException.ThrowIfNull(roleKeys);

        var normalizedUserId =
            userId.Trim();

        if (string.IsNullOrWhiteSpace(normalizedUserId))
        {
            throw new ArgumentException(
                "User ID must not be empty.",
                nameof(userId));
        }

        var normalizedRoleKeys =
            roleKeys
                .Where(role =>
                    !string.IsNullOrWhiteSpace(role))
                .Select(role =>
                    role.Trim().ToUpperInvariant())
                .Distinct()
                .ToArray();

        return GetInternalAsync(
            workflowInstanceId,
            normalizedUserId,
            normalizedRoleKeys,
            includeAllTasks,
            cancellationToken);
    }

    private async Task<WorkflowDetailItem?> GetInternalAsync(
        Guid workflowInstanceId,
        string? userId,
        IReadOnlyCollection<string> roleKeys,
        bool includeAllTasks,
        CancellationToken cancellationToken)
    {
        await using var db =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        var workflow = await (
            from instance in db.WorkflowInstances.AsNoTracking()

            join version in db.WorkflowVersions.AsNoTracking()
                on instance.WorkflowVersionId equals version.Id

            join definition in db.WorkflowDefinitions.AsNoTracking()
                on version.WorkflowDefinitionId equals definition.Id

            where instance.Id == workflowInstanceId

            select new
            {
                instance.Id,
                WorkflowName = definition.Name,
                instance.Subject,
                instance.ReferenceDate,
                instance.Status,
                instance.CreatedAt,
                instance.CreatedByUserId,
                instance.CompletedAt
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (workflow is null)
        {
            return null;
        }

        var properties =
            await db.WorkflowInstanceProperties
                .AsNoTracking()
                .Where(property =>
                    property.WorkflowInstanceId ==
                    workflowInstanceId)
                .OrderBy(property =>
                    property.Key)
                .ToDictionaryAsync(
                    property => property.Key,
                    property => property.Value,
                    cancellationToken);

        var totalTasks =
            await db.TaskInstances
                .AsNoTracking()
                .CountAsync(
                    task =>
                        task.WorkflowInstanceId ==
                        workflowInstanceId,
                    cancellationToken);

        var completedTasks =
            await db.TaskInstances
                .AsNoTracking()
                .CountAsync(
                    task =>
                        task.WorkflowInstanceId ==
                            workflowInstanceId
                        &&
                        (
                            task.Status ==
                                WorkflowTaskStatus.Completed
                            ||
                            task.Status ==
                                WorkflowTaskStatus.NotRequired
                        ),
                    cancellationToken);

        var taskQuery =
            from taskInstance in db.TaskInstances.AsNoTracking()

            join taskDefinition in db.TaskDefinitions.AsNoTracking()
                on taskInstance.TaskDefinitionId
                equals taskDefinition.Id

            where taskInstance.WorkflowInstanceId ==
                  workflowInstanceId

            select new
            {
                taskInstance.Id,
                taskDefinition.Key,
                taskDefinition.Title,
                taskDefinition.Description,
                taskDefinition.Phase,
                taskDefinition.SortOrder,
                taskInstance.AssignedRoleKey,
                taskDefinition.IsOptional,
                taskInstance.Status,
                taskInstance.AssignedUserId,
                taskInstance.Comment,
                taskInstance.CompletedByUserId,
                taskInstance.CompletedAt
            };

        if (!includeAllTasks)
        {
            taskQuery =
                taskQuery.Where(task =>
                    task.AssignedUserId == userId
                    ||
                    (
                        task.AssignedUserId == null
                        &&
                        roleKeys.Contains(
                            task.AssignedRoleKey)
                    ));
        }

        var taskData =
            await taskQuery
                .OrderBy(task =>
                    task.SortOrder)
                .ThenBy(task =>
                    task.Key)
                .ToListAsync(cancellationToken);

        var tasks =
            taskData
                .Select(task =>
                    new WorkflowTaskDetailItem(
                        task.Id,
                        task.Key,
                        task.Title,
                        task.Description,
                        task.Phase,
                        task.SortOrder,
                        task.AssignedRoleKey,
                        task.IsOptional,
                        (int)task.Status,
                        task.AssignedUserId,
                        task.Comment,
                        task.CompletedByUserId,
                        task.CompletedAt))
                .ToList();

        return new WorkflowDetailItem(
            workflow.Id,
            workflow.WorkflowName,
            workflow.Subject,
            workflow.ReferenceDate,
            (int)workflow.Status,
            workflow.CreatedAt,
            workflow.CreatedByUserId,
            workflow.CompletedAt,
            totalTasks,
            completedTasks,
            properties,
            tasks);
    }
}
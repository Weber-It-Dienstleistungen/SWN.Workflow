using Microsoft.EntityFrameworkCore;
using Swn.Workflow.Application;

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

    public async Task<WorkflowDetailItem?> GetByIdAsync(
        Guid workflowInstanceId,
        CancellationToken cancellationToken = default)
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

        var properties = await db.WorkflowInstanceProperties
            .AsNoTracking()
            .Where(property =>
                property.WorkflowInstanceId == workflowInstanceId)
            .OrderBy(property => property.Key)
            .ToDictionaryAsync(
                property => property.Key,
                property => property.Value,
                cancellationToken);

        var taskData = await (
            from taskInstance in db.TaskInstances.AsNoTracking()
            join taskDefinition in db.TaskDefinitions.AsNoTracking()
                on taskInstance.TaskDefinitionId equals taskDefinition.Id
            where taskInstance.WorkflowInstanceId == workflowInstanceId
            orderby taskDefinition.SortOrder, taskDefinition.Key
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
            })
            .ToListAsync(cancellationToken);

        var tasks = taskData
            .Select(task => new WorkflowTaskDetailItem(
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
            properties,
            tasks);
    }
}
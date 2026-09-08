using Microsoft.EntityFrameworkCore;
using Swn.Workflow.Application;
using Swn.Workflow.Domain;

namespace Swn.Workflow.Infrastructure;

public sealed class DashboardService : IDashboardService
{
    private readonly IDbContextFactory<WorkflowDbContext> _dbContextFactory;

    public DashboardService(
        IDbContextFactory<WorkflowDbContext> dbContextFactory)
    {
        ArgumentNullException.ThrowIfNull(dbContextFactory);

        _dbContextFactory = dbContextFactory;
    }

    public async Task<IReadOnlyList<WorkflowOverviewItem>>
        GetActiveWorkflowsAsync(
            CancellationToken cancellationToken = default)
    {
        await using var db =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        var items = await (
            from instance in db.WorkflowInstances.AsNoTracking()
            join version in db.WorkflowVersions.AsNoTracking()
                on instance.WorkflowVersionId equals version.Id
            join definition in db.WorkflowDefinitions.AsNoTracking()
                on version.WorkflowDefinitionId equals definition.Id
            where instance.Status == WorkflowStatus.Open
                  || instance.Status == WorkflowStatus.InProgress
            orderby instance.CreatedAt descending
            select new
            {
                instance.Id,
                WorkflowName = definition.Name,
                instance.Subject,
                instance.ReferenceDate,
                instance.Status,

                TotalTasks = db.TaskInstances.Count(
                    task =>
                        task.WorkflowInstanceId == instance.Id),

                CompletedTasks = db.TaskInstances.Count(
                    task =>
                        task.WorkflowInstanceId == instance.Id &&
                        (task.Status == WorkflowTaskStatus.Completed
                         || task.Status == WorkflowTaskStatus.NotRequired))
            })
            .ToListAsync(cancellationToken);

        return items
            .Select(item => new WorkflowOverviewItem(
                item.Id,
                item.WorkflowName,
                item.Subject,
                item.ReferenceDate,
                (int)item.Status,
                item.TotalTasks,
                item.CompletedTasks))
            .ToList();
    }

    public async Task<IReadOnlyList<AvailableWorkflowItem>>
        GetAvailableWorkflowsAsync(
            CancellationToken cancellationToken = default)
    {
        await using var db =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        return await db.WorkflowDefinitions
            .AsNoTracking()
            .Where(definition =>
                definition.IsActive &&
                db.WorkflowVersions.Any(
                    version =>
                        version.WorkflowDefinitionId == definition.Id &&
                        version.IsPublished))
            .OrderBy(definition => definition.Name)
            .Select(definition => new AvailableWorkflowItem(
                definition.Key,
                definition.Name,
                definition.Description))
            .ToListAsync(cancellationToken);
    }
}
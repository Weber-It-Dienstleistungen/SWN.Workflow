using Microsoft.EntityFrameworkCore;
using Swn.Workflow.Application;
using Swn.Workflow.Domain;

namespace Swn.Workflow.Infrastructure;

public sealed class WorkflowTaskService : IWorkflowTaskService
{
    private readonly IDbContextFactory<WorkflowDbContext> _dbContextFactory;

    public WorkflowTaskService(
        IDbContextFactory<WorkflowDbContext> dbContextFactory)
    {
        ArgumentNullException.ThrowIfNull(dbContextFactory);

        _dbContextFactory = dbContextFactory;
    }

    public async Task UpdateStatusAsync(
        Guid taskInstanceId,
        int status,
        string changedByUserId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(changedByUserId);

        var changedBy = changedByUserId.Trim();

        if (string.IsNullOrWhiteSpace(changedBy))
        {
            throw new ArgumentException(
                "Changing user must not be empty.",
                nameof(changedByUserId));
        }

        if (!Enum.IsDefined(typeof(WorkflowTaskStatus), status))
        {
            throw new ArgumentOutOfRangeException(
                nameof(status),
                status,
                "The supplied task status is invalid.");
        }

        var newStatus = (WorkflowTaskStatus)status;

        await using var db =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        var task = await db.TaskInstances
            .SingleOrDefaultAsync(
                task => task.Id == taskInstanceId,
                cancellationToken);

        if (task is null)
        {
            throw new InvalidOperationException(
                $"Task instance '{taskInstanceId}' was not found.");
        }

        var workflow = await db.WorkflowInstances
            .SingleOrDefaultAsync(
                workflow =>
                    workflow.Id == task.WorkflowInstanceId,
                cancellationToken);

        if (workflow is null)
        {
            throw new InvalidOperationException(
                $"Workflow instance '{task.WorkflowInstanceId}' was not found.");
        }

        if (workflow.Status == WorkflowStatus.Cancelled)
        {
            throw new InvalidOperationException(
                "Tasks of a cancelled workflow cannot be changed.");
        }

        task.Status = newStatus;

        if (newStatus == WorkflowTaskStatus.Completed)
        {
            task.CompletedAt ??= DateTime.UtcNow;
            task.CompletedByUserId ??= changedBy;
        }
        else
        {
            task.CompletedAt = null;
            task.CompletedByUserId = null;
        }

        var workflowTasks = await db.TaskInstances
            .Where(taskInstance =>
                taskInstance.WorkflowInstanceId == workflow.Id)
            .ToListAsync(cancellationToken);

        var allTasksCompleted =
            workflowTasks.Count > 0 &&
            workflowTasks.All(
                taskInstance =>
                    taskInstance.Status ==
                        WorkflowTaskStatus.Completed ||
                    taskInstance.Status ==
                        WorkflowTaskStatus.NotRequired);

        var anyTaskStarted =
            workflowTasks.Any(
                taskInstance =>
                    taskInstance.Status !=
                        WorkflowTaskStatus.Open);

        if (allTasksCompleted)
        {
            workflow.Status = WorkflowStatus.Completed;
            workflow.CompletedAt ??= DateTime.UtcNow;
        }
        else if (anyTaskStarted)
        {
            workflow.Status = WorkflowStatus.InProgress;
            workflow.CompletedAt = null;
        }
        else
        {
            workflow.Status = WorkflowStatus.Open;
            workflow.CompletedAt = null;
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
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
        IReadOnlyCollection<string> roleKeys,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(changedByUserId);
        ArgumentNullException.ThrowIfNull(roleKeys);

        var changedBy =
            NormalizeUserId(changedByUserId);

        var normalizedRoleKeys =
            NormalizeRoleKeys(roleKeys);

        if (!Enum.IsDefined(
            typeof(WorkflowTaskStatus),
            status))
        {
            throw new ArgumentOutOfRangeException(
                nameof(status),
                status,
                "The supplied task status is invalid.");
        }

        var newStatus =
            (WorkflowTaskStatus)status;

        await using var db =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        var task =
            await db.TaskInstances
                .SingleOrDefaultAsync(
                    task =>
                        task.Id ==
                        taskInstanceId,
                    cancellationToken);

        if (task is null)
        {
            throw new InvalidOperationException(
                $"Task instance '{taskInstanceId}' was not found.");
        }

        EnsureUserMayEditTask(
            task,
            changedBy,
            normalizedRoleKeys);

        var workflow =
            await db.WorkflowInstances
                .SingleOrDefaultAsync(
                    workflow =>
                        workflow.Id ==
                        task.WorkflowInstanceId,
                    cancellationToken);

        if (workflow is null)
        {
            throw new InvalidOperationException(
                $"Workflow instance '{task.WorkflowInstanceId}' was not found.");
        }

        EnsureWorkflowCanBeChanged(
            workflow);

        task.Status =
            newStatus;

        if (newStatus ==
            WorkflowTaskStatus.Completed)
        {
            task.CompletedAt =
                DateTime.UtcNow;

            task.CompletedByUserId =
                changedBy;
        }
        else
        {
            task.CompletedAt =
                null;

            task.CompletedByUserId =
                null;
        }

        var workflowTasks =
            await db.TaskInstances
                .Where(taskInstance =>
                    taskInstance.WorkflowInstanceId ==
                    workflow.Id)
                .ToListAsync(
                    cancellationToken);

        var allTasksCompleted =
            workflowTasks.Count > 0
            &&
            workflowTasks.All(
                taskInstance =>
                    taskInstance.Status ==
                        WorkflowTaskStatus.Completed
                    ||
                    taskInstance.Status ==
                        WorkflowTaskStatus.NotRequired);

        var anyTaskStarted =
            workflowTasks.Any(
                taskInstance =>
                    taskInstance.Status !=
                        WorkflowTaskStatus.Open);

        if (allTasksCompleted)
        {
            workflow.Status =
                WorkflowStatus.Completed;

            workflow.CompletedAt ??=
                DateTime.UtcNow;
        }
        else if (anyTaskStarted)
        {
            workflow.Status =
                WorkflowStatus.InProgress;

            workflow.CompletedAt =
                null;
        }
        else
        {
            workflow.Status =
                WorkflowStatus.Open;

            workflow.CompletedAt =
                null;
        }

        await db.SaveChangesAsync(
            cancellationToken);
    }

    public async Task UpdateCommentAsync(
        Guid taskInstanceId,
        string? comment,
        string changedByUserId,
        IReadOnlyCollection<string> roleKeys,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(changedByUserId);
        ArgumentNullException.ThrowIfNull(roleKeys);

        var changedBy =
            NormalizeUserId(changedByUserId);

        var normalizedRoleKeys =
            NormalizeRoleKeys(roleKeys);

        var normalizedComment =
            string.IsNullOrWhiteSpace(comment)
                ? null
                : comment.Trim();

        if (normalizedComment?.Length > 2000)
        {
            throw new ArgumentException(
                "Task comments must not exceed 2000 characters.",
                nameof(comment));
        }

        await using var db =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        var task =
            await db.TaskInstances
                .SingleOrDefaultAsync(
                    task =>
                        task.Id ==
                        taskInstanceId,
                    cancellationToken);

        if (task is null)
        {
            throw new InvalidOperationException(
                $"Task instance '{taskInstanceId}' was not found.");
        }

        EnsureUserMayEditTask(
            task,
            changedBy,
            normalizedRoleKeys);

        var workflow =
            await db.WorkflowInstances
                .SingleOrDefaultAsync(
                    workflow =>
                        workflow.Id ==
                        task.WorkflowInstanceId,
                    cancellationToken);

        if (workflow is null)
        {
            throw new InvalidOperationException(
                $"Workflow instance '{task.WorkflowInstanceId}' was not found.");
        }

        EnsureWorkflowCanBeChanged(
            workflow);

        task.Comment =
            normalizedComment;

        await db.SaveChangesAsync(
            cancellationToken);
    }

    private static string NormalizeUserId(
        string userId)
    {
        var normalizedUserId =
            userId.Trim();

        if (string.IsNullOrWhiteSpace(
            normalizedUserId))
        {
            throw new ArgumentException(
                "Changing user must not be empty.",
                nameof(userId));
        }

        return normalizedUserId;
    }

    private static string[] NormalizeRoleKeys(
        IReadOnlyCollection<string> roleKeys)
    {
        return roleKeys
            .Where(role =>
                !string.IsNullOrWhiteSpace(role))
            .Select(role =>
                role.Trim().ToUpperInvariant())
            .Distinct()
            .ToArray();
    }

    private static void EnsureUserMayEditTask(
        TaskInstance task,
        string userId,
        IReadOnlyCollection<string> roleKeys)
    {
        var isDirectlyAssigned =
            !string.IsNullOrWhiteSpace(
                task.AssignedUserId)
            &&
            string.Equals(
                task.AssignedUserId,
                userId,
                StringComparison.Ordinal);

        var isAssignedByRole =
            string.IsNullOrWhiteSpace(
                task.AssignedUserId)
            &&
            roleKeys.Contains(
                task.AssignedRoleKey,
                StringComparer.OrdinalIgnoreCase);

        if (isDirectlyAssigned ||
            isAssignedByRole)
        {
            return;
        }

        throw new UnauthorizedAccessException(
            "The current user is not allowed to edit this task.");
    }

    private static void EnsureWorkflowCanBeChanged(
        WorkflowInstance workflow)
    {
        if (workflow.Status ==
            WorkflowStatus.Cancelled)
        {
            throw new InvalidOperationException(
                "Tasks of a cancelled workflow cannot be changed.");
        }

        if (workflow.Status ==
            WorkflowStatus.Completed)
        {
            throw new InvalidOperationException(
                "Tasks of a completed workflow cannot be changed.");
        }
    }
}
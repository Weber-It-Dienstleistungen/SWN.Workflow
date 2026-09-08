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

        await db.SaveChangesAsync(cancellationToken);
    }
}
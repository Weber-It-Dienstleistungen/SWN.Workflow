using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Swn.Workflow.Application;
using Swn.Workflow.Domain;

namespace Swn.Workflow.Infrastructure;

public sealed class WorkflowNotificationService
    : IWorkflowNotificationService
{
    private const string WorkflowCompletedTriggerKey =
        "WORKFLOW:COMPLETED";

    private readonly IDbContextFactory<WorkflowDbContext>
        _dbContextFactory;

    private readonly UserManager<ApplicationUser>
        _userManager;

    public WorkflowNotificationService(
        IDbContextFactory<WorkflowDbContext> dbContextFactory,
        UserManager<ApplicationUser> userManager)
    {
        ArgumentNullException.ThrowIfNull(
            dbContextFactory);

        ArgumentNullException.ThrowIfNull(
            userManager);

        _dbContextFactory =
            dbContextFactory;

        _userManager =
            userManager;
    }

    public async Task<WorkflowNotificationQueueResult>
        QueueTaskAvailableAsync(
            Guid taskInstanceId,
            CancellationToken cancellationToken = default)
    {
        await using var db =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        var task =
            await db.TaskInstances
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item =>
                        item.Id ==
                        taskInstanceId,
                    cancellationToken);

        if (task is null)
        {
            throw new InvalidOperationException(
                $"Task instance '{taskInstanceId}' was not found.");
        }

        if (task.Status !=
            WorkflowTaskStatus.Open)
        {
            throw new InvalidOperationException(
                "Only an open task can create a task-available notification.");
        }

        var recipients =
            new List<ApplicationUser>();

        var skippedRecipientCount =
            0;

        if (!string.IsNullOrWhiteSpace(
            task.AssignedUserId))
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            var assignedUser =
                await _userManager
                    .FindByIdAsync(
                        task.AssignedUserId);

            cancellationToken
                .ThrowIfCancellationRequested();

            if (IsUsableRecipient(
                assignedUser))
            {
                recipients.Add(
                    assignedUser!);
            }
            else
            {
                skippedRecipientCount++;
            }
        }
        else
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            var roleUsers =
                await _userManager
                    .GetUsersInRoleAsync(
                        task.AssignedRoleKey);

            cancellationToken
                .ThrowIfCancellationRequested();

            foreach (var roleUser
                in roleUsers)
            {
                if (IsUsableRecipient(
                    roleUser))
                {
                    recipients.Add(
                        roleUser);
                }
                else
                {
                    skippedRecipientCount++;
                }
            }
        }

        var distinctRecipients =
            recipients
                .GroupBy(
                    user =>
                        user.Id,
                    StringComparer.Ordinal)
                .Select(group =>
                    group.First())
                .ToArray();

        var triggerKey =
            $"TASK:{task.Id:N}:AVAILABLE";

        var createdCount =
            0;

        var alreadyQueuedCount =
            0;

        foreach (var recipient
            in distinctRecipients)
        {
            var alreadyExists =
                await db.WorkflowNotifications
                    .AsNoTracking()
                    .AnyAsync(
                        notification =>
                            notification.WorkflowInstanceId ==
                                task.WorkflowInstanceId
                            &&
                            notification.RecipientUserId ==
                                recipient.Id
                            &&
                            notification.Type ==
                                WorkflowNotificationType.TaskAvailable
                            &&
                            notification.TriggerKey ==
                                triggerKey,
                        cancellationToken);

            if (alreadyExists)
            {
                alreadyQueuedCount++;

                continue;
            }

            db.WorkflowNotifications.Add(
                new WorkflowNotification
                {
                    Id =
                        Guid.NewGuid(),

                    WorkflowInstanceId =
                        task.WorkflowInstanceId,

                    TaskInstanceId =
                        task.Id,

                    RecipientUserId =
                        recipient.Id,

                    RecipientEmail =
                        recipient.Email!.Trim(),

                    Type =
                        WorkflowNotificationType.TaskAvailable,

                    TriggerKey =
                        triggerKey,

                    Status =
                        WorkflowNotificationStatus.Pending,

                    CreatedAt =
                        DateTime.UtcNow
                });

            createdCount++;
        }

        if (createdCount > 0)
        {
            await db.SaveChangesAsync(
                cancellationToken);
        }

        return new WorkflowNotificationQueueResult(
            createdCount,
            alreadyQueuedCount,
            skippedRecipientCount);
    }

    public async Task<WorkflowNotificationQueueResult>
        QueueWorkflowCompletedAsync(
            Guid workflowInstanceId,
            CancellationToken cancellationToken = default)
    {
        await using var db =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        var workflow =
            await db.WorkflowInstances
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item =>
                        item.Id ==
                        workflowInstanceId,
                    cancellationToken);

        if (workflow is null)
        {
            throw new InvalidOperationException(
                $"Workflow instance '{workflowInstanceId}' was not found.");
        }

        if (workflow.Status !=
            WorkflowStatus.Completed)
        {
            throw new InvalidOperationException(
                "Only a completed workflow can create a workflow-completed notification.");
        }

        cancellationToken
            .ThrowIfCancellationRequested();

        var initiator =
            await _userManager
                .FindByIdAsync(
                    workflow.CreatedByUserId);

        cancellationToken
            .ThrowIfCancellationRequested();

        if (!IsUsableRecipient(
            initiator))
        {
            return new WorkflowNotificationQueueResult(
                CreatedCount: 0,
                AlreadyQueuedCount: 0,
                SkippedRecipientCount: 1);
        }

        var alreadyExists =
            await db.WorkflowNotifications
                .AsNoTracking()
                .AnyAsync(
                    notification =>
                        notification.WorkflowInstanceId ==
                            workflow.Id
                        &&
                        notification.RecipientUserId ==
                            initiator!.Id
                        &&
                        notification.Type ==
                            WorkflowNotificationType.WorkflowCompleted
                        &&
                        notification.TriggerKey ==
                            WorkflowCompletedTriggerKey,
                    cancellationToken);

        if (alreadyExists)
        {
            return new WorkflowNotificationQueueResult(
                CreatedCount: 0,
                AlreadyQueuedCount: 1,
                SkippedRecipientCount: 0);
        }

        db.WorkflowNotifications.Add(
            new WorkflowNotification
            {
                Id =
                    Guid.NewGuid(),

                WorkflowInstanceId =
                    workflow.Id,

                TaskInstanceId =
                    null,

                RecipientUserId =
                    initiator!.Id,

                RecipientEmail =
                    initiator.Email!.Trim(),

                Type =
                    WorkflowNotificationType.WorkflowCompleted,

                TriggerKey =
                    WorkflowCompletedTriggerKey,

                Status =
                    WorkflowNotificationStatus.Pending,

                CreatedAt =
                    DateTime.UtcNow
            });

        await db.SaveChangesAsync(
            cancellationToken);

        return new WorkflowNotificationQueueResult(
            CreatedCount: 1,
            AlreadyQueuedCount: 0,
            SkippedRecipientCount: 0);
    }

    private static bool IsUsableRecipient(
        ApplicationUser? user)
    {
        return user is not null
            &&
            user.IsActive
            &&
            !string.IsNullOrWhiteSpace(
                user.Email);
    }
}
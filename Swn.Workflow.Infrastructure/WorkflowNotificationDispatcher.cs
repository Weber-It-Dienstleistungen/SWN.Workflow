using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Swn.Workflow.Application;
using Swn.Workflow.Domain;

namespace Swn.Workflow.Infrastructure;

public sealed class WorkflowNotificationDispatcher
    : IWorkflowNotificationDispatcher
{
    private readonly IDbContextFactory<WorkflowDbContext>
        _dbContextFactory;

    private readonly IEmailSender
        _emailSender;

    private readonly ILogger<WorkflowNotificationDispatcher>
        _logger;

    public WorkflowNotificationDispatcher(
        IDbContextFactory<WorkflowDbContext> dbContextFactory,
        IEmailSender emailSender,
        ILogger<WorkflowNotificationDispatcher> logger)
    {
        ArgumentNullException.ThrowIfNull(
            dbContextFactory);

        ArgumentNullException.ThrowIfNull(
            emailSender);

        ArgumentNullException.ThrowIfNull(
            logger);

        _dbContextFactory =
            dbContextFactory;

        _emailSender =
            emailSender;

        _logger =
            logger;
    }

    public async Task<WorkflowNotificationDispatchResult>
        DispatchPendingTaskAvailableAsync(
            CancellationToken cancellationToken = default)
    {
        await using var db =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        var pendingNotifications =
            await db.WorkflowNotifications
                .Where(notification =>
                    notification.Status ==
                        WorkflowNotificationStatus.Pending
                    &&
                    notification.Type ==
                        WorkflowNotificationType.TaskAvailable)
                .OrderBy(notification =>
                    notification.CreatedAt)
                .ToListAsync(
                    cancellationToken);

        if (pendingNotifications.Count == 0)
        {
            return new WorkflowNotificationDispatchResult(
                ProcessedGroupCount: 0,
                SentGroupCount: 0,
                FailedGroupCount: 0,
                SentNotificationCount: 0,
                FailedNotificationCount: 0);
        }

        var failedNotificationCount =
            0;

        var notificationsWithoutTask =
            pendingNotifications
                .Where(notification =>
                    notification.TaskInstanceId is null)
                .ToArray();

        if (notificationsWithoutTask.Length > 0)
        {
            var now =
                DateTime.UtcNow;

            foreach (var notification
                in notificationsWithoutTask)
            {
                MarkAsFailed(
                    notification,
                    now,
                    "Die zugehörige Aufgabe ist nicht mehr vorhanden.");
            }

            failedNotificationCount +=
                notificationsWithoutTask.Length;

            await db.SaveChangesAsync(
                cancellationToken);
        }

        var validNotifications =
            pendingNotifications
                .Where(notification =>
                    notification.TaskInstanceId is not null)
                .ToArray();

        if (validNotifications.Length == 0)
        {
            return new WorkflowNotificationDispatchResult(
                ProcessedGroupCount: 0,
                SentGroupCount: 0,
                FailedGroupCount: 0,
                SentNotificationCount: 0,
                FailedNotificationCount:
                    failedNotificationCount);
        }

        var taskInstanceIds =
            validNotifications
                .Select(notification =>
                    notification.TaskInstanceId!.Value)
                .Distinct()
                .ToArray();

        var taskDetails =
            await (
                from taskInstance
                    in db.TaskInstances.AsNoTracking()

                join taskDefinition
                    in db.TaskDefinitions.AsNoTracking()
                    on taskInstance.TaskDefinitionId
                    equals taskDefinition.Id

                join workflowInstance
                    in db.WorkflowInstances.AsNoTracking()
                    on taskInstance.WorkflowInstanceId
                    equals workflowInstance.Id

                join workflowVersion
                    in db.WorkflowVersions.AsNoTracking()
                    on workflowInstance.WorkflowVersionId
                    equals workflowVersion.Id

                join workflowDefinition
                    in db.WorkflowDefinitions.AsNoTracking()
                    on workflowVersion.WorkflowDefinitionId
                    equals workflowDefinition.Id

                where
                    taskInstanceIds.Contains(
                        taskInstance.Id)

                select new TaskNotificationDetail(
                    taskInstance.Id,
                    workflowInstance.Id,
                    workflowDefinition.Name,
                    workflowInstance.Subject,
                    workflowInstance.ReferenceDate,
                    taskDefinition.Key,
                    taskDefinition.Title,
                    taskDefinition.SortOrder)
            )
            .ToListAsync(
                cancellationToken);

        var taskDetailsById =
            taskDetails.ToDictionary(
                item =>
                    item.TaskInstanceId);

        var groups =
            validNotifications
                .GroupBy(notification =>
                    new NotificationGroupKey(
                        notification.WorkflowInstanceId,
                        notification.RecipientUserId,
                        notification.RecipientEmail))
                .ToArray();

        var processedGroupCount =
            0;

        var sentGroupCount =
            0;

        var failedGroupCount =
            0;

        var sentNotificationCount =
            0;

        foreach (var group
            in groups)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            processedGroupCount++;

            var groupNotifications =
                group.ToArray();

            var missingTaskDetail =
                groupNotifications.Any(
                    notification =>
                        notification.TaskInstanceId is null
                        ||
                        !taskDetailsById.ContainsKey(
                            notification.TaskInstanceId.Value));

            if (missingTaskDetail)
            {
                var now =
                    DateTime.UtcNow;

                foreach (var notification
                    in groupNotifications)
                {
                    MarkAsFailed(
                        notification,
                        now,
                        "Mindestens eine zugehörige Aufgabe konnte nicht geladen werden.");
                }

                failedGroupCount++;

                failedNotificationCount +=
                    groupNotifications.Length;

                await db.SaveChangesAsync(
                    cancellationToken);

                continue;
            }

            var user =
                await db.Users
                    .AsNoTracking()
                    .SingleOrDefaultAsync(
                        item =>
                            item.Id ==
                            group.Key.RecipientUserId,
                        cancellationToken);

            if (user is null ||
                !user.IsActive)
            {
                var now =
                    DateTime.UtcNow;

                foreach (var notification
                    in groupNotifications)
                {
                    MarkAsFailed(
                        notification,
                        now,
                        "Der Empfänger ist nicht mehr als aktiver Benutzer verfügbar.");
                }

                failedGroupCount++;

                failedNotificationCount +=
                    groupNotifications.Length;

                await db.SaveChangesAsync(
                    cancellationToken);

                continue;
            }

            var tasks =
                groupNotifications
                    .Select(notification =>
                        taskDetailsById[
                            notification.TaskInstanceId!.Value])
                    .OrderBy(task =>
                        task.SortOrder)
                    .ThenBy(task =>
                        task.TaskKey)
                    .ToArray();

            var firstTask =
                tasks[0];

            var recipientName =
                GetRecipientName(
                    user);

            var emailMessage =
                CreateTaskAvailableEmail(
                    group.Key.RecipientEmail,
                    recipientName,
                    firstTask.WorkflowName,
                    firstTask.WorkflowSubject,
                    firstTask.ReferenceDate,
                    tasks);

            var attemptAt =
                DateTime.UtcNow;

            try
            {
                await _emailSender.SendAsync(
                    emailMessage,
                    cancellationToken);

                foreach (var notification
                    in groupNotifications)
                {
                    notification.AttemptCount++;

                    notification.LastAttemptAt =
                        attemptAt;

                    notification.Status =
                        WorkflowNotificationStatus.Sent;

                    notification.SentAt =
                        DateTime.UtcNow;

                    notification.LastError =
                        null;
                }

                await db.SaveChangesAsync(
                    cancellationToken);

                sentGroupCount++;

                sentNotificationCount +=
                    groupNotifications.Length;

                _logger.LogInformation(
                    "Sent bundled task notification for workflow " +
                    "{WorkflowInstanceId} to user {RecipientUserId}. " +
                    "Notifications: {NotificationCount}.",
                    group.Key.WorkflowInstanceId,
                    group.Key.RecipientUserId,
                    groupNotifications.Length);
            }
            catch (Exception exception)
            {
                var errorMessage =
                    GetErrorMessage(
                        exception);

                foreach (var notification
                    in groupNotifications)
                {
                    MarkAsFailed(
                        notification,
                        attemptAt,
                        errorMessage);
                }

                await db.SaveChangesAsync(
                    cancellationToken);

                failedGroupCount++;

                failedNotificationCount +=
                    groupNotifications.Length;

                _logger.LogError(
                    exception,
                    "Could not send bundled task notification " +
                    "for workflow {WorkflowInstanceId} " +
                    "to user {RecipientUserId}.",
                    group.Key.WorkflowInstanceId,
                    group.Key.RecipientUserId);
            }
        }

        return new WorkflowNotificationDispatchResult(
            processedGroupCount,
            sentGroupCount,
            failedGroupCount,
            sentNotificationCount,
            failedNotificationCount);
    }

    private static EmailMessage
        CreateTaskAvailableEmail(
            string recipientAddress,
            string? recipientName,
            string workflowName,
            string workflowSubject,
            DateOnly referenceDate,
            IReadOnlyCollection<TaskNotificationDetail> tasks)
    {
        var text =
            new StringBuilder();

        if (string.IsNullOrWhiteSpace(
            recipientName))
        {
            text.AppendLine(
                "Guten Tag,");
        }
        else
        {
            text.Append(
                "Hallo ");

            text.Append(
                recipientName);

            text.AppendLine(
                ",");
        }

        text.AppendLine();

        if (tasks.Count == 1)
        {
            text.AppendLine(
                "für Sie steht eine neue Aufgabe im SWN Workflow bereit:");
        }
        else
        {
            text.Append(
                "für Sie stehen ");

            text.Append(
                tasks.Count);

            text.AppendLine(
                " neue Aufgaben im SWN Workflow bereit:");
        }

        text.AppendLine();

        text.Append(
            "Vorgang: ");

        text.AppendLine(
            workflowSubject);

        text.Append(
            "Workflow: ");

        text.AppendLine(
            workflowName);

        text.Append(
            "Stichtag: ");

        text.AppendLine(
            referenceDate.ToString(
                "dd.MM.yyyy"));

        text.AppendLine();

        foreach (var task
            in tasks)
        {
            text.Append(
                "- ");

            text.AppendLine(
                task.TaskTitle);
        }

        text.AppendLine();

        text.AppendLine(
            "Bitte öffnen Sie SWN Workflow, um die Aufgaben zu bearbeiten.");

        text.AppendLine();

        text.AppendLine(
            "Viele Grüße");

        text.AppendLine(
            "SWN Workflow");

        return new EmailMessage(
            recipientAddress,
            recipientName,
            $"SWN Workflow – Neue Aufgaben: {workflowSubject}",
            text.ToString());
    }

    private static string? GetRecipientName(
        ApplicationUser user)
    {
        if (!string.IsNullOrWhiteSpace(
            user.DisplayName))
        {
            return user.DisplayName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(
            user.UserName))
        {
            return user.UserName.Trim();
        }

        return null;
    }

    private static void MarkAsFailed(
        WorkflowNotification notification,
        DateTime attemptAt,
        string errorMessage)
    {
        notification.AttemptCount++;

        notification.LastAttemptAt =
            attemptAt;

        notification.Status =
            WorkflowNotificationStatus.Failed;

        notification.SentAt =
            null;

        notification.LastError =
            errorMessage.Length <= 2000
                ? errorMessage
                : errorMessage[..2000];
    }

    private static string GetErrorMessage(
        Exception exception)
    {
        var message =
            exception.Message?.Trim();

        if (string.IsNullOrWhiteSpace(
            message))
        {
            message =
                exception.GetType().Name;
        }

        return message.Length <= 2000
            ? message
            : message[..2000];
    }

    private sealed record NotificationGroupKey(
        Guid WorkflowInstanceId,
        string RecipientUserId,
        string RecipientEmail);

    private sealed record TaskNotificationDetail(
        Guid TaskInstanceId,
        Guid WorkflowInstanceId,
        string WorkflowName,
        string WorkflowSubject,
        DateOnly ReferenceDate,
        string TaskKey,
        string TaskTitle,
        int SortOrder);
}
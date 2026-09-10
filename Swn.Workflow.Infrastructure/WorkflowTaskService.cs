using Microsoft.EntityFrameworkCore;
using Swn.Workflow.Application;
using Swn.Workflow.Domain;

namespace Swn.Workflow.Infrastructure;

public sealed class WorkflowTaskService : IWorkflowTaskService
{
    private readonly IDbContextFactory<WorkflowDbContext> _dbContextFactory;
    private readonly ISecretProtector _secretProtector;

    public WorkflowTaskService(
        IDbContextFactory<WorkflowDbContext> dbContextFactory,
        ISecretProtector secretProtector)
    {
        ArgumentNullException.ThrowIfNull(dbContextFactory);
        ArgumentNullException.ThrowIfNull(secretProtector);

        _dbContextFactory = dbContextFactory;
        _secretProtector = secretProtector;
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

        if (newStatus ==
            WorkflowTaskStatus.Completed)
        {
            await EnsureRequiredFieldsHaveValuesAsync(
                db,
                task,
                cancellationToken);
        }

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

    public async Task UpdateFieldValueAsync(
        Guid taskInstanceId,
        string fieldKey,
        string? value,
        string changedByUserId,
        IReadOnlyCollection<string> roleKeys,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fieldKey);
        ArgumentNullException.ThrowIfNull(changedByUserId);
        ArgumentNullException.ThrowIfNull(roleKeys);

        var normalizedFieldKey =
            fieldKey.Trim();

        if (string.IsNullOrWhiteSpace(
            normalizedFieldKey))
        {
            throw new ArgumentException(
                "Field key must not be empty.",
                nameof(fieldKey));
        }

        if (normalizedFieldKey.Length > 100)
        {
            throw new ArgumentException(
                "Field key must not exceed 100 characters.",
                nameof(fieldKey));
        }

        var changedBy =
            NormalizeUserId(changedByUserId);

        var normalizedRoleKeys =
            NormalizeRoleKeys(roleKeys);

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

        var fieldDefinition =
            await db.TaskFieldDefinitions
                .SingleOrDefaultAsync(
                    field =>
                        field.TaskDefinitionId ==
                            task.TaskDefinitionId
                        &&
                        field.Key ==
                            normalizedFieldKey,
                    cancellationToken);

        if (fieldDefinition is null)
        {
            throw new InvalidOperationException(
                $"Task field '{normalizedFieldKey}' was not found.");
        }

        switch (fieldDefinition.FieldType)
        {
            case TaskFieldType.Text:
                {
                    if (value?.Length > 2000)
                    {
                        throw new ArgumentException(
                            "Text field values must not exceed 2000 characters.",
                            nameof(value));
                    }

                    var existingValue =
                        await db.TaskInstanceFieldValues
                            .SingleOrDefaultAsync(
                                storedValue =>
                                    storedValue.TaskInstanceId ==
                                        taskInstanceId
                                    &&
                                    storedValue.Key ==
                                        fieldDefinition.Key,
                                cancellationToken);

                    var existingSecret =
                        await db.TaskInstanceSecrets
                            .SingleOrDefaultAsync(
                                secret =>
                                    secret.TaskInstanceId ==
                                        taskInstanceId
                                    &&
                                    secret.Key ==
                                        fieldDefinition.Key,
                                cancellationToken);

                    if (existingSecret is not null)
                    {
                        db.TaskInstanceSecrets.Remove(
                            existingSecret);
                    }

                    if (string.IsNullOrEmpty(value))
                    {
                        if (existingValue is not null)
                        {
                            db.TaskInstanceFieldValues.Remove(
                                existingValue);
                        }
                    }
                    else if (existingValue is null)
                    {
                        db.TaskInstanceFieldValues.Add(
                            new TaskInstanceFieldValue
                            {
                                Id = Guid.NewGuid(),
                                TaskInstanceId = taskInstanceId,
                                Key = fieldDefinition.Key,
                                Value = value
                            });
                    }
                    else
                    {
                        existingValue.Value =
                            value;
                    }

                    break;
                }

            case TaskFieldType.Secret:
                {
                    var existingSecret =
                        await db.TaskInstanceSecrets
                            .SingleOrDefaultAsync(
                                secret =>
                                    secret.TaskInstanceId ==
                                        taskInstanceId
                                    &&
                                    secret.Key ==
                                        fieldDefinition.Key,
                                cancellationToken);

                    var existingValue =
                        await db.TaskInstanceFieldValues
                            .SingleOrDefaultAsync(
                                storedValue =>
                                    storedValue.TaskInstanceId ==
                                        taskInstanceId
                                    &&
                                    storedValue.Key ==
                                        fieldDefinition.Key,
                                cancellationToken);

                    if (existingValue is not null)
                    {
                        db.TaskInstanceFieldValues.Remove(
                            existingValue);
                    }

                    if (string.IsNullOrEmpty(value))
                    {
                        if (existingSecret is not null)
                        {
                            db.TaskInstanceSecrets.Remove(
                                existingSecret);
                        }
                    }
                    else
                    {
                        var encryptedValue =
                            _secretProtector.Protect(
                                value);

                        if (existingSecret is null)
                        {
                            db.TaskInstanceSecrets.Add(
                                new TaskInstanceSecret
                                {
                                    Id = Guid.NewGuid(),
                                    TaskInstanceId = taskInstanceId,
                                    Key = fieldDefinition.Key,
                                    Label = fieldDefinition.Label,
                                    EncryptedValue = encryptedValue
                                });
                        }
                        else
                        {
                            existingSecret.Label =
                                fieldDefinition.Label;

                            existingSecret.EncryptedValue =
                                encryptedValue;
                        }
                    }

                    break;
                }

            default:
                throw new InvalidOperationException(
                    $"Task field type '{fieldDefinition.FieldType}' is not supported.");
        }

        await db.SaveChangesAsync(
            cancellationToken);
    }

    private static async Task EnsureRequiredFieldsHaveValuesAsync(
        WorkflowDbContext db,
        TaskInstance task,
        CancellationToken cancellationToken)
    {
        var requiredFields =
            await db.TaskFieldDefinitions
                .AsNoTracking()
                .Where(field =>
                    field.TaskDefinitionId ==
                        task.TaskDefinitionId
                    &&
                    field.IsRequired)
                .OrderBy(field =>
                    field.SortOrder)
                .ToListAsync(
                    cancellationToken);

        if (requiredFields.Count == 0)
        {
            return;
        }

        var textValues =
            await db.TaskInstanceFieldValues
                .AsNoTracking()
                .Where(value =>
                    value.TaskInstanceId ==
                        task.Id)
                .ToListAsync(
                    cancellationToken);

        var secretValues =
            await db.TaskInstanceSecrets
                .AsNoTracking()
                .Where(secret =>
                    secret.TaskInstanceId ==
                        task.Id)
                .ToListAsync(
                    cancellationToken);

        var missingLabels =
            new List<string>();

        foreach (var field in requiredFields)
        {
            var hasValue =
                field.FieldType switch
                {
                    TaskFieldType.Text =>
                        textValues.Any(value =>
                            string.Equals(
                                value.Key,
                                field.Key,
                                StringComparison.OrdinalIgnoreCase)
                            &&
                            !string.IsNullOrWhiteSpace(
                                value.Value)),

                    TaskFieldType.Secret =>
                        secretValues.Any(secret =>
                            string.Equals(
                                secret.Key,
                                field.Key,
                                StringComparison.OrdinalIgnoreCase)),

                    _ =>
                        throw new InvalidOperationException(
                            $"Task field type '{field.FieldType}' is not supported.")
                };

            if (!hasValue)
            {
                missingLabels.Add(
                    field.Label);
            }
        }

        if (missingLabels.Count > 0)
        {
            throw new WorkflowTaskValidationException(
                "Die Aufgabe kann nicht abgeschlossen werden. "
                +
                "Folgende Pflichtfelder fehlen: "
                +
                string.Join(
                    ", ",
                    missingLabels));
        }
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
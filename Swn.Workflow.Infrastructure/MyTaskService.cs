using Microsoft.EntityFrameworkCore;
using Swn.Workflow.Application;
using Swn.Workflow.Domain;

namespace Swn.Workflow.Infrastructure;

public sealed class MyTaskService : IMyTaskService
{
    private readonly IDbContextFactory<WorkflowDbContext> _dbContextFactory;
    private readonly ISecretProtector _secretProtector;

    public MyTaskService(
        IDbContextFactory<WorkflowDbContext> dbContextFactory,
        ISecretProtector secretProtector)
    {
        ArgumentNullException.ThrowIfNull(dbContextFactory);
        ArgumentNullException.ThrowIfNull(secretProtector);

        _dbContextFactory = dbContextFactory;
        _secretProtector = secretProtector;
    }

    public async Task<IReadOnlyList<MyTaskItem>> GetOpenTasksAsync(
        string userId,
        IReadOnlyCollection<string> roleKeys,
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

        await using var db =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        var taskData =
            await (
                from taskInstance in db.TaskInstances.AsNoTracking()

                join taskDefinition in db.TaskDefinitions.AsNoTracking()
                    on taskInstance.TaskDefinitionId
                    equals taskDefinition.Id

                join workflowInstance in db.WorkflowInstances.AsNoTracking()
                    on taskInstance.WorkflowInstanceId
                    equals workflowInstance.Id

                join workflowVersion in db.WorkflowVersions.AsNoTracking()
                    on workflowInstance.WorkflowVersionId
                    equals workflowVersion.Id

                join workflowDefinition in db.WorkflowDefinitions.AsNoTracking()
                    on workflowVersion.WorkflowDefinitionId
                    equals workflowDefinition.Id

                where
                    (
                        workflowInstance.Status ==
                            WorkflowStatus.Open
                        ||
                        workflowInstance.Status ==
                            WorkflowStatus.InProgress
                    )
                    &&
                    taskInstance.Status !=
                        WorkflowTaskStatus.Completed
                    &&
                    taskInstance.Status !=
                        WorkflowTaskStatus.NotRequired
                    &&
                    (
                        taskInstance.AssignedUserId ==
                            normalizedUserId
                        ||
                        (
                            taskInstance.AssignedUserId == null
                            &&
                            normalizedRoleKeys.Contains(
                                taskInstance.AssignedRoleKey)
                        )
                    )

                orderby
                    workflowInstance.ReferenceDate,
                    workflowDefinition.Name,
                    workflowInstance.Subject,
                    taskDefinition.SortOrder

                select new
                {
                    TaskInstanceId =
                        taskInstance.Id,

                    taskInstance.TaskDefinitionId,

                    WorkflowInstanceId =
                        workflowInstance.Id,

                    WorkflowName =
                        workflowDefinition.Name,

                    workflowInstance.Subject,

                    workflowInstance.ReferenceDate,

                    taskDefinition.Key,

                    taskDefinition.Title,

                    taskDefinition.Description,

                    taskDefinition.Phase,

                    taskDefinition.SortOrder,

                    taskInstance.AssignedRoleKey,

                    taskInstance.Status,

                    taskInstance.Comment
                })
            .ToListAsync(cancellationToken);

        if (taskData.Count == 0)
        {
            return Array.Empty<MyTaskItem>();
        }

        var taskDefinitionIds =
            taskData
                .Select(task =>
                    task.TaskDefinitionId)
                .Distinct()
                .ToArray();

        var taskInstanceIds =
            taskData
                .Select(task =>
                    task.TaskInstanceId)
                .Distinct()
                .ToArray();

        var fieldDefinitions =
            await db.TaskFieldDefinitions
                .AsNoTracking()
                .Where(field =>
                    taskDefinitionIds.Contains(
                        field.TaskDefinitionId))
                .OrderBy(field =>
                    field.SortOrder)
                .ThenBy(field =>
                    field.Key)
                .ToListAsync(cancellationToken);

        var fieldValues =
            await db.TaskInstanceFieldValues
                .AsNoTracking()
                .Where(value =>
                    taskInstanceIds.Contains(
                        value.TaskInstanceId))
                .ToListAsync(cancellationToken);

        var secretValues =
            await db.TaskInstanceSecrets
                .AsNoTracking()
                .Where(secret =>
                    taskInstanceIds.Contains(
                        secret.TaskInstanceId))
                .ToListAsync(cancellationToken);

        var fieldDefinitionsByTask =
            fieldDefinitions
                .GroupBy(field =>
                    field.TaskDefinitionId)
                .ToDictionary(
                    group => group.Key,
                    group => group.ToArray());

        var fieldValuesByTask =
            fieldValues
                .GroupBy(value =>
                    value.TaskInstanceId)
                .ToDictionary(
                    group => group.Key,
                    group => group.ToDictionary(
                        value => value.Key,
                        value => value.Value,
                        StringComparer.OrdinalIgnoreCase));

        var secretValuesByTask =
            secretValues
                .GroupBy(secret =>
                    secret.TaskInstanceId)
                .ToDictionary(
                    group => group.Key,
                    group => group.ToDictionary(
                        secret => secret.Key,
                        secret => secret.EncryptedValue,
                        StringComparer.OrdinalIgnoreCase));

        var result =
            new List<MyTaskItem>(
                taskData.Count);

        foreach (var task in taskData)
        {
            var fields =
                new List<MyTaskFieldItem>();

            if (fieldDefinitionsByTask.TryGetValue(
                task.TaskDefinitionId,
                out var definitions))
            {
                fieldValuesByTask.TryGetValue(
                    task.TaskInstanceId,
                    out var values);

                secretValuesByTask.TryGetValue(
                    task.TaskInstanceId,
                    out var secrets);

                foreach (var field in definitions)
                {
                    string? value = null;

                    switch (field.FieldType)
                    {
                        case TaskFieldType.Text:
                            if (values is not null
                                &&
                                values.TryGetValue(
                                    field.Key,
                                    out var storedValue))
                            {
                                value = storedValue;
                            }

                            break;

                        case TaskFieldType.Secret:
                            if (secrets is not null
                                &&
                                secrets.TryGetValue(
                                    field.Key,
                                    out var encryptedValue))
                            {
                                value =
                                    _secretProtector.Unprotect(
                                        encryptedValue);
                            }

                            break;

                        default:
                            throw new InvalidOperationException(
                                $"Task field type '{field.FieldType}' is not supported.");
                    }

                    fields.Add(
                        new MyTaskFieldItem(
                            field.Key,
                            field.Label,
                            (int)field.FieldType,
                            field.SortOrder,
                            field.IsRequired,
                            value));
                }
            }

            result.Add(
                new MyTaskItem(
                    task.TaskInstanceId,
                    task.WorkflowInstanceId,
                    task.WorkflowName,
                    task.Subject,
                    task.ReferenceDate,
                    task.Key,
                    task.Title,
                    task.Description,
                    task.Phase,
                    task.SortOrder,
                    task.AssignedRoleKey,
                    (int)task.Status,
                    task.Comment,
                    fields));
        }

        return result;
    }
}
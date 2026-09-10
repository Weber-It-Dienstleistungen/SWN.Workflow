using Microsoft.EntityFrameworkCore;
using Swn.Workflow.Domain;

namespace Swn.Workflow.Infrastructure;

public static class TaskFieldSeedData
{
    public static async Task InitializeAsync(
        IDbContextFactory<WorkflowDbContext> dbContextFactory)
    {
        ArgumentNullException.ThrowIfNull(dbContextFactory);

        await using var db =
            await dbContextFactory.CreateDbContextAsync();

        await SeedCredentialFieldsAsync(
            db,
            "ONBOARDING",
            "ONB-008");

        await SeedCredentialFieldsAsync(
            db,
            "ONBOARDING",
            "ONB-014");

        await db.SaveChangesAsync();
    }

    private static async Task SeedCredentialFieldsAsync(
        WorkflowDbContext db,
        string workflowKey,
        string taskKey)
    {
        var taskDefinition =
            await (
                from task in db.TaskDefinitions
                join version in db.WorkflowVersions
                    on task.WorkflowVersionId equals version.Id
                join workflow in db.WorkflowDefinitions
                    on version.WorkflowDefinitionId equals workflow.Id
                where
                    workflow.Key == workflowKey
                    && version.IsPublished
                    && task.Key == taskKey
                orderby version.VersionNumber descending
                select task)
            .FirstOrDefaultAsync();

        if (taskDefinition is null)
        {
            throw new InvalidOperationException(
                $"Task definition '{taskKey}' of workflow '{workflowKey}' was not found.");
        }

        await AddOrUpdateFieldAsync(
            db,
            taskDefinition.Id,
            "USERNAME",
            "Benutzername",
            TaskFieldType.Text,
            1,
            true);

        await AddOrUpdateFieldAsync(
            db,
            taskDefinition.Id,
            "PASSWORD",
            "Passwort",
            TaskFieldType.Secret,
            2,
            true);
    }

    private static async Task AddOrUpdateFieldAsync(
        WorkflowDbContext db,
        Guid taskDefinitionId,
        string key,
        string label,
        TaskFieldType fieldType,
        int sortOrder,
        bool isRequired)
    {
        var existingField =
            await db.TaskFieldDefinitions
                .SingleOrDefaultAsync(
                    field =>
                        field.TaskDefinitionId == taskDefinitionId
                        && field.Key == key);

        if (existingField is null)
        {
            db.TaskFieldDefinitions.Add(
                new TaskFieldDefinition
                {
                    Id = Guid.NewGuid(),
                    TaskDefinitionId = taskDefinitionId,
                    Key = key,
                    Label = label,
                    FieldType = fieldType,
                    SortOrder = sortOrder,
                    IsRequired = isRequired
                });

            return;
        }

        existingField.Label = label;
        existingField.FieldType = fieldType;
        existingField.SortOrder = sortOrder;
        existingField.IsRequired = isRequired;
    }
}
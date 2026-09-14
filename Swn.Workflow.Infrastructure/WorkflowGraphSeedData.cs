using Microsoft.EntityFrameworkCore;
using Swn.Workflow.Domain;

namespace Swn.Workflow.Infrastructure;

public static class WorkflowGraphSeedData
{
    public static async Task InitializeAsync(
        IDbContextFactory<WorkflowDbContext> dbContextFactory)
    {
        ArgumentNullException.ThrowIfNull(dbContextFactory);

        await using var db =
            await dbContextFactory.CreateDbContextAsync();

        var onboardingDefinition =
            await db.WorkflowDefinitions
                .SingleAsync(
                    definition =>
                        definition.Key == "ONBOARDING");

        var onboardingVersion =
            await db.WorkflowVersions
                .Where(version =>
                    version.WorkflowDefinitionId ==
                        onboardingDefinition.Id
                    &&
                    version.IsPublished)
                .OrderByDescending(version =>
                    version.VersionNumber)
                .FirstAsync();

        var decisionTask =
            await db.TaskDefinitions
                .SingleAsync(
                    task =>
                        task.WorkflowVersionId ==
                            onboardingVersion.Id
                        &&
                        task.Key == "ONB-001");

        var keyIssueTask =
            await db.TaskDefinitions
                .SingleAsync(
                    task =>
                        task.WorkflowVersionId ==
                            onboardingVersion.Id
                        &&
                        task.Key == "ONB-021");

        decisionTask.TaskType =
            TaskType.Decision;

        await AddOrUpdateDecisionOptionAsync(
            db,
            decisionTask.Id,
            "YES",
            "Ja",
            1);

        await AddOrUpdateDecisionOptionAsync(
            db,
            decisionTask.Id,
            "NO",
            "Nein",
            2);

        await AddOrUpdateTransitionAsync(
            db,
            decisionTask.Id,
            keyIssueTask.Id,
            "YES");

        await db.SaveChangesAsync();
    }

    private static async Task
        AddOrUpdateDecisionOptionAsync(
            WorkflowDbContext db,
            Guid taskDefinitionId,
            string key,
            string label,
            int sortOrder)
    {
        var option =
            await db.TaskDecisionOptionDefinitions
                .SingleOrDefaultAsync(
                    existing =>
                        existing.TaskDefinitionId ==
                            taskDefinitionId
                        &&
                        existing.Key == key);

        if (option is null)
        {
            db.TaskDecisionOptionDefinitions.Add(
                new TaskDecisionOptionDefinition
                {
                    Id = Guid.NewGuid(),
                    TaskDefinitionId =
                        taskDefinitionId,
                    Key = key,
                    Label = label,
                    SortOrder = sortOrder
                });

            return;
        }

        option.Label = label;
        option.SortOrder = sortOrder;
    }

    private static async Task
        AddOrUpdateTransitionAsync(
            WorkflowDbContext db,
            Guid fromTaskDefinitionId,
            Guid toTaskDefinitionId,
            string? requiredDecisionOutcomeKey)
    {
        var transition =
            await db.TaskTransitionDefinitions
                .SingleOrDefaultAsync(
                    existing =>
                        existing.FromTaskDefinitionId ==
                            fromTaskDefinitionId
                        &&
                        existing.ToTaskDefinitionId ==
                            toTaskDefinitionId);

        if (transition is null)
        {
            db.TaskTransitionDefinitions.Add(
                new TaskTransitionDefinition
                {
                    Id = Guid.NewGuid(),
                    FromTaskDefinitionId =
                        fromTaskDefinitionId,
                    ToTaskDefinitionId =
                        toTaskDefinitionId,
                    RequiredDecisionOutcomeKey =
                        requiredDecisionOutcomeKey
                });

            return;
        }

        transition.RequiredDecisionOutcomeKey =
            requiredDecisionOutcomeKey;
    }
}
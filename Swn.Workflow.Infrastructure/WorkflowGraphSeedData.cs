using Microsoft.EntityFrameworkCore;
using Swn.Workflow.Domain;

namespace Swn.Workflow.Infrastructure;

public static class WorkflowGraphSeedData
{
    public static async Task InitializeAsync(
        IDbContextFactory<WorkflowDbContext> dbContextFactory)
    {
        ArgumentNullException.ThrowIfNull(
            dbContextFactory);

        await using var db =
            await dbContextFactory
                .CreateDbContextAsync();

        var onboardingDefinition =
            await db.WorkflowDefinitions
                .SingleAsync(
                    definition =>
                        definition.Key ==
                        "ONBOARDING");

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

        var tasks =
            await db.TaskDefinitions
                .Where(task =>
                    task.WorkflowVersionId ==
                        onboardingVersion.Id)
                .ToDictionaryAsync(
                    task =>
                        task.Key);

        ConfigureExistingDecisionTask(
            tasks,
            "ONB-001",
            1);

        ConfigureExistingDecisionTask(
            tasks,
            "ONB-002",
            2);

        ConfigureExistingDecisionTask(
            tasks,
            "ONB-003",
            3);

        ConfigureExistingDecisionTask(
            tasks,
            "ONB-004",
            4);

        ConfigureExistingDecisionTask(
            tasks,
            "ONB-005",
            5);

        ConfigureExistingDecisionTask(
            tasks,
            "ONB-006",
            6);

        ConfigureExistingDecisionTask(
            tasks,
            "ONB-007",
            7);

        AddOrUpdateTaskDefinition(
            db,
            tasks,
            onboardingVersion.Id,
            "ONB-D008",
            "Bedarf an Rechnerzugang klären",
            "Festlegen, ob für den Mitarbeiter ein Rechnerzugang eingerichtet werden muss.",
            "Vorbereitung vor Eintritt",
            8,
            "SUPERVISOR",
            false,
            TaskType.Decision);

        AddOrUpdateTaskDefinition(
            db,
            tasks,
            onboardingVersion.Id,
            "ONB-D010",
            "Bedarf an PC-Arbeitsplatz klären",
            "Festlegen, ob ein stationärer PC-Arbeitsplatz benötigt wird.",
            "Vorbereitung vor Eintritt",
            9,
            "SUPERVISOR",
            false,
            TaskType.Decision);

        AddOrUpdateTaskDefinition(
            db,
            tasks,
            onboardingVersion.Id,
            "ONB-D011",
            "Bedarf an mobilem PC-System klären",
            "Festlegen, ob ein mobiles PC-System benötigt wird.",
            "Vorbereitung vor Eintritt",
            10,
            "SUPERVISOR",
            false,
            TaskType.Decision);

        AddOrUpdateTaskDefinition(
            db,
            tasks,
            onboardingVersion.Id,
            "ONB-D012",
            "Bedarf an Leitsystem-Berechtigung klären",
            "Festlegen, ob eine Benutzerberechtigung für das Leitsystem benötigt wird.",
            "Vorbereitung vor Eintritt",
            11,
            "SUPERVISOR",
            false,
            TaskType.Decision);

        AddOrUpdateTaskDefinition(
            db,
            tasks,
            onboardingVersion.Id,
            "ONB-D013",
            "Bedarf an E-Mail-Account klären",
            "Festlegen, ob für den Mitarbeiter ein E-Mail-Account eingerichtet werden muss.",
            "Vorbereitung vor Eintritt",
            12,
            "SUPERVISOR",
            false,
            TaskType.Decision);

        AddOrUpdateTaskDefinition(
            db,
            tasks,
            onboardingVersion.Id,
            "ONB-D014",
            "Bedarf an Schleupen-Zugang klären",
            "Festlegen, ob ein Schleupen-Zugang benötigt wird.",
            "Vorbereitung vor Eintritt",
            13,
            "SUPERVISOR",
            false,
            TaskType.Decision);

        AddOrUpdateTaskDefinition(
            db,
            tasks,
            onboardingVersion.Id,
            "ONB-D015",
            "Bedarf an Arbeitsplatztelefon klären",
            "Festlegen, ob ein Arbeitsplatztelefon benötigt wird.",
            "Vorbereitung vor Eintritt",
            14,
            "SUPERVISOR",
            false,
            TaskType.Decision);

        AddOrUpdateTaskDefinition(
            db,
            tasks,
            onboardingVersion.Id,
            "ONB-D016",
            "Bedarf an Mobiltelefon klären",
            "Festlegen, ob ein Mobiltelefon benötigt wird.",
            "Vorbereitung vor Eintritt",
            15,
            "SUPERVISOR",
            false,
            TaskType.Decision);

        AddOrUpdateTaskDefinition(
            db,
            tasks,
            onboardingVersion.Id,
            "ONB-D019",
            "Bedarf an Zeiterfassungs-Transponder klären",
            "Festlegen, ob ein Transponder zur Zeiterfassung benötigt wird.",
            "Vorbereitung vor Eintritt",
            16,
            "SUPERVISOR",
            false,
            TaskType.Decision);

        AddOrUpdateTaskDefinition(
            db,
            tasks,
            onboardingVersion.Id,
            "ONB-D022",
            "Bedarf an Alarmanlagen-Transponder klären",
            "Festlegen, ob ein Transponder zur Bedienung der Alarmanlage im Hallenbad benötigt wird.",
            "Vorbereitung vor Eintritt",
            17,
            "SUPERVISOR",
            false,
            TaskType.Decision);

        AddOrUpdateTaskDefinition(
            db,
            tasks,
            onboardingVersion.Id,
            "ONB-D025",
            "Bedarf an Informationssicherheits-Schulung klären",
            "Festlegen, ob die Schulung zur Informationssicherheit durchgeführt werden muss.",
            "Vorbereitung vor Eintritt",
            18,
            "SUPERVISOR",
            false,
            TaskType.Decision);

        AddOrUpdateTaskDefinition(
            db,
            tasks,
            onboardingVersion.Id,
            "ONB-026",
            "Zusätzliche Software und Zugänge bereitstellen",
            "Die vom Vorgesetzten festgelegte zusätzliche Software beziehungsweise zusätzliche Zugänge bereitstellen. Falls vorhandene Lizenzen nicht ausreichen, ist die Beschaffung zu klären.",
            "Vorbereitung vor Eintritt",
            102,
            "IT",
            true,
            TaskType.Work);

        AddOrUpdateTaskDefinition(
            db,
            tasks,
            onboardingVersion.Id,
            "ONB-027",
            "Zusätzliche Ausstattung bereitstellen",
            "Die vom Vorgesetzten festgelegte zusätzliche Ausstattung für den Arbeitsplatz bereitstellen beziehungsweise die Beschaffung veranlassen.",
            "Vorbereitung vor Eintritt",
            103,
            "WAREHOUSE",
            true,
            TaskType.Work);

        ConfigureExistingWorkTask(
            tasks,
            "ONB-008",
            108);

        ConfigureExistingWorkTask(
            tasks,
            "ONB-009",
            109);

        ConfigureExistingWorkTask(
            tasks,
            "ONB-010",
            110);

        ConfigureExistingWorkTask(
            tasks,
            "ONB-011",
            111);

        ConfigureExistingWorkTask(
            tasks,
            "ONB-012",
            112);

        ConfigureExistingWorkTask(
            tasks,
            "ONB-013",
            113);

        ConfigureExistingWorkTask(
            tasks,
            "ONB-014",
            114);

        ConfigureExistingWorkTask(
            tasks,
            "ONB-015",
            115);

        ConfigureExistingWorkTask(
            tasks,
            "ONB-016",
            116);

        ConfigureExistingWorkTask(
            tasks,
            "ONB-017",
            117);

        ConfigureExistingWorkTask(
            tasks,
            "ONB-018",
            118);

        ConfigureExistingWorkTask(
            tasks,
            "ONB-019",
            119);

        ConfigureExistingWorkTask(
            tasks,
            "ONB-020",
            120);

        ConfigureExistingWorkTask(
            tasks,
            "ONB-021",
            121);

        ConfigureExistingWorkTask(
            tasks,
            "ONB-022",
            122);

        ConfigureExistingWorkTask(
            tasks,
            "ONB-023",
            123);

        ConfigureExistingWorkTask(
            tasks,
            "ONB-024",
            124);

        ConfigureExistingWorkTask(
            tasks,
            "ONB-025",
            125);

        var branches =
            new[]
            {
                new DecisionBranch(
                    "ONB-001",
                    "ONB-021"),

                new DecisionBranch(
                    "ONB-002",
                    "ONB-026"),

                new DecisionBranch(
                    "ONB-003",
                    "ONB-027"),

                new DecisionBranch(
                    "ONB-004",
                    "ONB-023"),

                new DecisionBranch(
                    "ONB-005",
                    "ONB-024"),

                new DecisionBranch(
                    "ONB-006",
                    "ONB-009"),

                new DecisionBranch(
                    "ONB-007",
                    "ONB-017"),

                new DecisionBranch(
                    "ONB-D008",
                    "ONB-008"),

                new DecisionBranch(
                    "ONB-D010",
                    "ONB-010"),

                new DecisionBranch(
                    "ONB-D011",
                    "ONB-011"),

                new DecisionBranch(
                    "ONB-D012",
                    "ONB-012"),

                new DecisionBranch(
                    "ONB-D013",
                    "ONB-013"),

                new DecisionBranch(
                    "ONB-D014",
                    "ONB-014"),

                new DecisionBranch(
                    "ONB-D015",
                    "ONB-015"),

                new DecisionBranch(
                    "ONB-D016",
                    "ONB-016"),

                new DecisionBranch(
                    "ONB-D019",
                    "ONB-019"),

                new DecisionBranch(
                    "ONB-D022",
                    "ONB-022"),

                new DecisionBranch(
                    "ONB-D025",
                    "ONB-025")
            };

        foreach (var branch in branches)
        {
            var decisionTask =
                GetRequiredTask(
                    tasks,
                    branch.DecisionTaskKey);

            var targetTask =
                GetRequiredTask(
                    tasks,
                    branch.TargetTaskKey);

            decisionTask.TaskType =
                TaskType.Decision;

            decisionTask.IsOptional =
                false;

            await AddYesNoOptionsAsync(
                db,
                decisionTask.Id);

            await AddOrUpdateTransitionAsync(
                db,
                decisionTask.Id,
                targetTask.Id,
                "YES");
        }

        await db.SaveChangesAsync();
    }

    private static void ConfigureExistingDecisionTask(
        IReadOnlyDictionary<string, TaskDefinition> tasks,
        string key,
        int sortOrder)
    {
        var task =
            GetRequiredTask(
                tasks,
                key);

        task.TaskType =
            TaskType.Decision;

        task.IsOptional =
            false;

        task.SortOrder =
            sortOrder;
    }

    private static void ConfigureExistingWorkTask(
        IReadOnlyDictionary<string, TaskDefinition> tasks,
        string key,
        int sortOrder)
    {
        var task =
            GetRequiredTask(
                tasks,
                key);

        task.TaskType =
            TaskType.Work;

        task.SortOrder =
            sortOrder;
    }

    private static TaskDefinition AddOrUpdateTaskDefinition(
        WorkflowDbContext db,
        IDictionary<string, TaskDefinition> tasks,
        Guid workflowVersionId,
        string key,
        string title,
        string description,
        string phase,
        int sortOrder,
        string assignedRoleKey,
        bool isOptional,
        TaskType taskType)
    {
        if (!tasks.TryGetValue(
            key,
            out var task))
        {
            task =
                new TaskDefinition
                {
                    Id =
                        Guid.NewGuid(),

                    WorkflowVersionId =
                        workflowVersionId,

                    Key =
                        key
                };

            db.TaskDefinitions.Add(
                task);

            tasks.Add(
                key,
                task);
        }

        task.Title =
            title;

        task.Description =
            description;

        task.Phase =
            phase;

        task.SortOrder =
            sortOrder;

        task.AssignedRoleKey =
            assignedRoleKey;

        task.IsOptional =
            isOptional;

        task.TaskType =
            taskType;

        return task;
    }

    private static TaskDefinition GetRequiredTask(
        IReadOnlyDictionary<string, TaskDefinition> tasks,
        string key)
    {
        if (tasks.TryGetValue(
            key,
            out var task))
        {
            return task;
        }

        throw new InvalidOperationException(
            $"Task definition '{key}' was not found.");
    }

    private static async Task AddYesNoOptionsAsync(
        WorkflowDbContext db,
        Guid taskDefinitionId)
    {
        await AddOrUpdateDecisionOptionAsync(
            db,
            taskDefinitionId,
            "YES",
            "Ja",
            1);

        await AddOrUpdateDecisionOptionAsync(
            db,
            taskDefinitionId,
            "NO",
            "Nein",
            2);
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
                        existing.Key ==
                            key);

        if (option is null)
        {
            db.TaskDecisionOptionDefinitions.Add(
                new TaskDecisionOptionDefinition
                {
                    Id =
                        Guid.NewGuid(),

                    TaskDefinitionId =
                        taskDefinitionId,

                    Key =
                        key,

                    Label =
                        label,

                    SortOrder =
                        sortOrder
                });

            return;
        }

        option.Label =
            label;

        option.SortOrder =
            sortOrder;
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
                    Id =
                        Guid.NewGuid(),

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

    private sealed record DecisionBranch(
        string DecisionTaskKey,
        string TargetTaskKey);
}
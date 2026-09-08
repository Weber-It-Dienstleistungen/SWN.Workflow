using Microsoft.EntityFrameworkCore;
using Swn.Workflow.Domain;

namespace Swn.Workflow.Infrastructure;

public static class WorkflowSeedData
{
    private static readonly Guid OnboardingDefinitionId =
        Guid.Parse("5f8977a3-55a8-4ac8-a42a-47bb35d435b1");

    private static readonly Guid OnboardingVersionId =
        Guid.Parse("83c0e516-f431-44d5-97dd-50475855444f");

    private static readonly Guid OffboardingDefinitionId =
        Guid.Parse("54e42460-1e53-4931-a916-e09932881aa2");

    private static readonly Guid OffboardingVersionId =
        Guid.Parse("ad353392-b085-4efd-a44e-7221d19d8dd8");

    public static async Task InitializeAsync(
        IDbContextFactory<WorkflowDbContext> dbContextFactory)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync();

        await SeedOnboardingAsync(db);
        await SeedOffboardingAsync(db);

        await db.SaveChangesAsync();
    }

    private static async Task SeedOnboardingAsync(WorkflowDbContext db)
    {
        var definitionExists = await db.WorkflowDefinitions
            .AnyAsync(x => x.Key == "ONBOARDING");

        if (!definitionExists)
        {
            db.WorkflowDefinitions.Add(new WorkflowDefinition
            {
                Id = OnboardingDefinitionId,
                Key = "ONBOARDING",
                Name = "Einstellung bzw. Arbeitsplatzwechsel",
                Description =
                    "Workflow für die Einstellung neuer Mitarbeitender oder einen Arbeitsplatzwechsel.",
                IsActive = true
            });
        }

        var versionExists = await db.WorkflowVersions
            .AnyAsync(x => x.Id == OnboardingVersionId);

        if (!versionExists)
        {
            db.WorkflowVersions.Add(new WorkflowVersion
            {
                Id = OnboardingVersionId,
                WorkflowDefinitionId = OnboardingDefinitionId,
                VersionNumber = 1,
                IsPublished = true
            });
        }

        var existingTaskKeys = await db.TaskDefinitions
            .Where(x => x.WorkflowVersionId == OnboardingVersionId)
            .Select(x => x.Key)
            .ToListAsync();

        var existingKeys = existingTaskKeys.ToHashSet(
            StringComparer.OrdinalIgnoreCase);

        var tasks = new[]
        {
            new SeedTask(
                "ONB-001",
                "Schließberechtigung definieren",
                "Schließberechtigung festlegen. Das separate Dokument zur Schlüsselausgabe bleibt außerhalb des Workflow-Systems.",
                "Vorbereitung vor Eintritt",
                1,
                "SUPERVISOR",
                true),

            new SeedTask(
                "ONB-002",
                "Zusätzlich benötigte Software klären",
                "Zusätzlich benötigte Software und Zugänge festlegen. Falls vorhandene Lizenzen nicht ausreichen, ist die Beschaffung zu klären.",
                "Vorbereitung vor Eintritt",
                2,
                "SUPERVISOR",
                true),

            new SeedTask(
                "ONB-003",
                "Zusätzliche Ausstattung festlegen",
                "Zusätzlich benötigte Ausstattung für den Arbeitsplatz festlegen.",
                "Vorbereitung vor Eintritt",
                3,
                "SUPERVISOR",
                true),

            new SeedTask(
                "ONB-004",
                "Bedarf an Berufskleidung klären",
                "Festlegen, ob Berufskleidung benötigt wird.",
                "Vorbereitung vor Eintritt",
                4,
                "SUPERVISOR",
                true),

            new SeedTask(
                "ONB-005",
                "Bedarf an Mitarbeiterausweis klären",
                "Festlegen, ob die Ausgabe eines Mitarbeiterausweises erforderlich ist.",
                "Vorbereitung vor Eintritt",
                5,
                "SUPERVISOR",
                true),

            new SeedTask(
                "ONB-006",
                "Benötigte Schulungen definieren",
                "Benötigte Schulungen entsprechend dem Tätigkeitsprofil im Schulungssystem sam® definieren.",
                "Vorbereitung vor Eintritt",
                6,
                "SUPERVISOR",
                true),

            new SeedTask(
                "ONB-007",
                "Zutrittskontrolle definieren",
                "Erforderliche Zutrittsberechtigungen beziehungsweise den ZK-Plan definieren.",
                "Vorbereitung vor Eintritt",
                7,
                "SUPERVISOR",
                true),

            new SeedTask(
                "ONB-008",
                "Rechnerzugang einrichten",
                "Rechnerzugang für den Mitarbeiter einrichten. Benutzername und Passwort können im Workflow erfasst werden. Das Passwort wird verschlüsselt gespeichert.",
                "Vorbereitung vor Eintritt",
                8,
                "IT",
                true),

            new SeedTask(
                "ONB-009",
                "Benutzer und Schulungen in sam® anlegen",
                "Benutzer und vorgesehene Schulungen im Schulungssystem sam® anlegen.",
                "Vorbereitung vor Eintritt",
                9,
                "IT",
                true),

            new SeedTask(
                "ONB-010",
                "PC-Arbeitsplatz bereitstellen",
                "Erforderlichen stationären PC-Arbeitsplatz bereitstellen.",
                "Vorbereitung vor Eintritt",
                10,
                "IT",
                true),

            new SeedTask(
                "ONB-011",
                "Mobiles PC-System bereitstellen",
                "Falls erforderlich ein mobiles PC-System bereitstellen.",
                "Vorbereitung vor Eintritt",
                11,
                "IT",
                true),

            new SeedTask(
                "ONB-012",
                "Benutzerberechtigung Leitsystem einrichten",
                "Erforderliche Benutzerberechtigung für das Leitsystem einrichten. Benutzername und Passwort können im Workflow erfasst werden. Das Passwort wird verschlüsselt gespeichert.",
                "Vorbereitung vor Eintritt",
                12,
                "IT",
                true),

            new SeedTask(
                "ONB-013",
                "E-Mail-Account einrichten",
                "E-Mail-Account einrichten. Benutzername und Passwort können im Workflow erfasst werden. Das Passwort wird verschlüsselt gespeichert.",
                "Vorbereitung vor Eintritt",
                13,
                "IT",
                true),

            new SeedTask(
                "ONB-014",
                "Schleupen-Zugang einrichten",
                "Erforderlichen Schleupen-Zugang einrichten. Benutzername und Passwort können im Workflow erfasst werden. Das Passwort wird verschlüsselt gespeichert.",
                "Vorbereitung vor Eintritt",
                14,
                "ORGANIZATION",
                true),

            new SeedTask(
                "ONB-015",
                "Arbeitsplatztelefon bereitstellen",
                "Falls erforderlich ein Arbeitsplatztelefon für den vorgesehenen Raum bereitstellen.",
                "Vorbereitung vor Eintritt",
                15,
                "TELENEC",
                true),

            new SeedTask(
                "ONB-016",
                "Mobiltelefon bereitstellen",
                "Falls erforderlich ein Mobiltelefon bereitstellen.",
                "Vorbereitung vor Eintritt",
                16,
                "TELENEC",
                true),

            new SeedTask(
                "ONB-017",
                "Zutrittskontrolle freigeben",
                "Zutrittskontrolle freigeben und den festgelegten ZK-Plan hinterlegen.",
                "Vorbereitung vor Eintritt",
                17,
                "HR",
                true),

            new SeedTask(
                "ONB-018",
                "Einstellungsunterlagen aushändigen",
                "Erforderliche Einstellungsunterlagen einschließlich relevanter Richtlinien aushändigen und den Erhalt bestätigen lassen.",
                "Vorbereitung vor Eintritt",
                18,
                "HR",
                false),

            new SeedTask(
                "ONB-019",
                "Transponder zur Zeiterfassung ausgeben",
                "Transponder zur Zeiterfassung aktivieren und an den Mitarbeiter ausgeben.",
                "Zum Eintrittstermin",
                19,
                "HR",
                false),

            new SeedTask(
                "ONB-020",
                "Anlage 1 zum Bewerbungsverfahren prüfen",
                "Prüfen, ob die Anlage 1 der Richtlinie zur Personalauswahl im Bewerbungsverfahren vollständig ist.",
                "Zum Eintrittstermin",
                20,
                "HR",
                false),

            new SeedTask(
                "ONB-021",
                "Schlüssel ausgeben",
                "Schlüssel entsprechend der festgelegten Schließberechtigung ausgeben und den Erhalt bestätigen lassen.",
                "Zum Eintrittstermin",
                21,
                "EXECUTIVE_SECRETARIAT",
                true),

            new SeedTask(
                "ONB-022",
                "Alarmanlagen-Transponder Hallenbad ausgeben",
                "Falls erforderlich den Transponder zur Bedienung der Alarmanlage im Hallenbad ausgeben.",
                "Zum Eintrittstermin",
                22,
                "TECHNICAL_MANAGEMENT",
                true),

            new SeedTask(
                "ONB-023",
                "Berufskleidung ausgeben",
                "Falls erforderlich Berufskleidung ausgeben oder bestellen, wenn sie nicht auf Lager ist.",
                "Zum Eintrittstermin",
                23,
                "WAREHOUSE",
                true),

            new SeedTask(
                "ONB-024",
                "Mitarbeiterausweis ausgeben",
                "Falls erforderlich den Mitarbeiterausweis an den Mitarbeiter ausgeben.",
                "Zum Eintrittstermin",
                24,
                "SUPERVISOR",
                true),

            new SeedTask(
                "ONB-025",
                "Schulung zur Informationssicherheit durchführen",
                "Schulung zur Informationssicherheit durchführen.",
                "Zum Eintrittstermin",
                25,
                "INFORMATION_SECURITY",
                false)
        };

        AddMissingTasks(
            db,
            OnboardingVersionId,
            tasks,
            existingKeys);
    }

    private static async Task SeedOffboardingAsync(WorkflowDbContext db)
    {
        var definitionExists = await db.WorkflowDefinitions
            .AnyAsync(x => x.Key == "OFFBOARDING");

        if (!definitionExists)
        {
            db.WorkflowDefinitions.Add(new WorkflowDefinition
            {
                Id = OffboardingDefinitionId,
                Key = "OFFBOARDING",
                Name = "Austritt bzw. Weggang",
                Description =
                    "Workflow für den Austritt oder Weggang von Mitarbeitenden.",
                IsActive = true
            });
        }

        var versionExists = await db.WorkflowVersions
            .AnyAsync(x => x.Id == OffboardingVersionId);

        if (!versionExists)
        {
            db.WorkflowVersions.Add(new WorkflowVersion
            {
                Id = OffboardingVersionId,
                WorkflowDefinitionId = OffboardingDefinitionId,
                VersionNumber = 1,
                IsPublished = true
            });
        }

        var existingTaskKeys = await db.TaskDefinitions
            .Where(x => x.WorkflowVersionId == OffboardingVersionId)
            .Select(x => x.Key)
            .ToListAsync();

        var existingKeys = existingTaskKeys.ToHashSet(
            StringComparer.OrdinalIgnoreCase);

        // Die Positionen 1, 6, 9, 15 und 17 sind im vorliegenden
        // Laufzettel ohne lesbaren Arbeitsschritt und werden deshalb
        // bewusst nicht als Aufgaben erfunden.

        var tasks = new[]
        {
            new SeedTask(
                "OFF-002",
                "Transponder Alarmanlage Hallenbad klären",
                "Vor dem Austritt klären, ob ein Transponder für die Alarmanlage im Hallenbad vorhanden ist.",
                "Vorbereitung vor Austritt",
                2,
                "SUPERVISOR",
                true),

            new SeedTask(
                "OFF-003",
                "Mitarbeiterausweis klären",
                "Vor dem Austritt klären, ob ein Mitarbeiterausweis vorhanden ist.",
                "Vorbereitung vor Austritt",
                3,
                "SUPERVISOR",
                true),

            new SeedTask(
                "OFF-004",
                "Transponder Aida klären",
                "Vor dem Austritt klären, ob ein Transponder für die Zeiterfassung vorhanden ist.",
                "Vorbereitung vor Austritt",
                4,
                "SUPERVISOR",
                true),

            new SeedTask(
                "OFF-005",
                "Zusätzliche Software und Zugänge klären",
                "Vor dem Austritt klären, welche zusätzliche Software beziehungsweise zusätzliche Zugänge vorhanden sind, beispielsweise Schleupen.",
                "Vorbereitung vor Austritt",
                5,
                "SUPERVISOR",
                true),

            new SeedTask(
                "OFF-007",
                "Berufskleidung klären",
                "Vor dem Austritt klären, ob ausgegebene Berufskleidung vorhanden ist.",
                "Vorbereitung vor Austritt",
                7,
                "SUPERVISOR",
                true),

            new SeedTask(
                "OFF-008",
                "Mitarbeiter über Daten und Rückgaben informieren",
                "Mitarbeiter darüber informieren, dass relevante Daten übergeben, irrelevante Daten gelöscht und rückgabepflichtige Gegenstände zurückgegeben werden müssen.",
                "Vorbereitung vor Austritt",
                8,
                "HR",
                false),

            new SeedTask(
                "OFF-010",
                "Alarmanlagen-Transponder Hallenbad einziehen",
                "Transponder zur Bedienung der Alarmanlage im Hallenbad einziehen.",
                "Bei Arbeitsende",
                10,
                "TECHNICAL_MANAGEMENT",
                true),

            new SeedTask(
                "OFF-011",
                "Mitarbeiterausweis einziehen",
                "Mitarbeiterausweis einziehen und an die zuständige Abteilung weitergeben.",
                "Bei Arbeitsende",
                11,
                "SUPERVISOR",
                true),

            new SeedTask(
                "OFF-012",
                "Transponder zur Zeiterfassung deaktivieren",
                "Transponder zur Zeiterfassung deaktivieren und einziehen.",
                "Bei Arbeitsende",
                12,
                "HR",
                true),

            new SeedTask(
                "OFF-013",
                "Auf Verschwiegenheit hinweisen",
                "Mitarbeiter auf die weitere Einhaltung der Verschwiegenheit hinweisen.",
                "Bei Arbeitsende",
                13,
                "HR",
                false),

            new SeedTask(
                "OFF-014",
                "Schleupen-Benutzer deaktivieren",
                "Schleupen-Benutzer deaktivieren.",
                "Bei Arbeitsende",
                14,
                "ORGANIZATION",
                true),

            new SeedTask(
                "OFF-016",
                "Leitsystem-Benutzerberechtigung deaktivieren",
                "Benutzerberechtigung für das Leitsystem deaktivieren.",
                "Bei Arbeitsende",
                16,
                "CONTROL_SYSTEM",
                true),

            new SeedTask(
                "OFF-018",
                "Berufskleidung einziehen",
                "Ausgegebene Berufskleidung einziehen.",
                "Bei Arbeitsende",
                18,
                "WAREHOUSE",
                true)
        };

        AddMissingTasks(
            db,
            OffboardingVersionId,
            tasks,
            existingKeys);
    }

    private static void AddMissingTasks(
        WorkflowDbContext db,
        Guid workflowVersionId,
        IEnumerable<SeedTask> tasks,
        HashSet<string> existingKeys)
    {
        foreach (var task in tasks)
        {
            if (existingKeys.Contains(task.Key))
            {
                continue;
            }

            db.TaskDefinitions.Add(new TaskDefinition
            {
                Id = Guid.NewGuid(),
                WorkflowVersionId = workflowVersionId,
                Key = task.Key,
                Title = task.Title,
                Description = task.Description,
                Phase = task.Phase,
                SortOrder = task.SortOrder,
                AssignedRoleKey = task.AssignedRoleKey,
                IsOptional = task.IsOptional
            });
        }
    }

    private sealed record SeedTask(
        string Key,
        string Title,
        string Description,
        string Phase,
        int SortOrder,
        string AssignedRoleKey,
        bool IsOptional);
}
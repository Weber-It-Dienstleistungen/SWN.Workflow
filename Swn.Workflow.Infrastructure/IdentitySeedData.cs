using Microsoft.AspNetCore.Identity;

namespace Swn.Workflow.Infrastructure;

public static class IdentitySeedData
{
    private const string InitialPassword = "Demo1234";

    private const string BootstrapAdministratorUserName =
        "prozess.projektsteuerung";

    private const string UserAdministratorRole =
        "USER_ADMIN";

    public static async Task InitializeAsync(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        ArgumentNullException.ThrowIfNull(userManager);
        ArgumentNullException.ThrowIfNull(roleManager);

        var roles = new[]
        {
            "SUPERVISOR",
            "HR",
            "IT",
            "ORGANIZATION",
            "TELENEC",
            "EXECUTIVE_SECRETARIAT",
            "TECHNICAL_MANAGEMENT",
            "WAREHOUSE",
            "INFORMATION_SECURITY",
            "CONTROL_SYSTEM",
            UserAdministratorRole
        };

        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(
                roleName))
            {
                var result =
                    await roleManager.CreateAsync(
                        new IdentityRole(
                            roleName));

                EnsureSucceeded(
                    result,
                    $"Rolle '{roleName}' konnte nicht angelegt werden.");
            }
        }

        var users = new[]
        {
            new UserSeedDefinition(
                "hr.demo",
                "personalwesen",
                "Personalwesen",
                new[]
                {
                    "HR"
                }),

            new UserSeedDefinition(
                "it.demo",
                "informationstechnik",
                "Informationstechnik",
                new[]
                {
                    "IT"
                }),

            new UserSeedDefinition(
                "organisation.demo",
                BootstrapAdministratorUserName,
                "Prozess- und Projektsteuerung",
                new[]
                {
                    "ORGANIZATION",
                    UserAdministratorRole
                }),

            new UserSeedDefinition(
                "telenec.demo",
                "telenec",
                "Telenec",
                new[]
                {
                    "TELENEC"
                }),

            new UserSeedDefinition(
                "lager.demo",
                "einkauf.lager",
                "Einkauf/Lager",
                new[]
                {
                    "WAREHOUSE"
                }),

            new UserSeedDefinition(
                "leitung.demo",
                "geschaeftsfuehrung",
                "Geschäftsführung",
                new[]
                {
                    "SUPERVISOR"
                }),

            new UserSeedDefinition(
                null,
                "sekretariat",
                "Sekretariat",
                new[]
                {
                    "EXECUTIVE_SECRETARIAT"
                }),

            new UserSeedDefinition(
                null,
                "hauptabteilungsleitung.kaufmaennische.abteilung",
                "Kaufm. Abteilung – Hauptabteilungsleitung",
                new[]
                {
                    "SUPERVISOR"
                }),

            new UserSeedDefinition(
                null,
                "hauptabteilungsleitung.technische.abteilung",
                "Technische Abteilung – Hauptabteilungsleitung",
                new[]
                {
                    "SUPERVISOR",
                    "TECHNICAL_MANAGEMENT"
                }),

            new UserSeedDefinition(
                null,
                "abteilungsleitung.netz.technische.anlagen",
                "Netz und techn. Anlagen – Abteilungsleitung",
                new[]
                {
                    "SUPERVISOR"
                }),

            new UserSeedDefinition(
                null,
                "bereichsleitung.strom",
                "Strom – Bereichsleitung",
                new[]
                {
                    "SUPERVISOR"
                }),

            new UserSeedDefinition(
                null,
                "bereichsleitung.gw.waerme",
                "G/W und Wärme – Bereichsleitung",
                new[]
                {
                    "SUPERVISOR"
                }),

            new UserSeedDefinition(
                null,
                "abteilungsleitung.telenec",
                "Telenec – Abteilungsleitung",
                new[]
                {
                    "SUPERVISOR"
                }),

            new UserSeedDefinition(
                null,
                "informationssicherheitsbeauftragter.ext",
                "Informationssicherheitsbeauftragter (ext.)",
                new[]
                {
                    "INFORMATION_SECURITY"
                }),

            new UserSeedDefinition(
                null,
                "leitsystem",
                "Leitsystem (Workflowbegriff – Zuordnung offen)",
                new[]
                {
                    "CONTROL_SYSTEM"
                })
        };

        foreach (var definition in users)
        {
            await CreateOrMigrateUserAsync(
                userManager,
                definition);
        }

        await EnsureBootstrapAdministratorAsync(
            userManager);
    }

    private static async Task CreateOrMigrateUserAsync(
        UserManager<ApplicationUser> userManager,
        UserSeedDefinition definition)
    {
        var user =
            await userManager.FindByNameAsync(
                definition.UserName);

        ApplicationUser? legacyUser =
            null;

        if (!string.IsNullOrWhiteSpace(
            definition.LegacyUserName))
        {
            legacyUser =
                await userManager.FindByNameAsync(
                    definition.LegacyUserName);
        }

        if (user is not null &&
            legacyUser is not null &&
            !string.Equals(
                user.Id,
                legacyUser.Id,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Sowohl der neue Benutzer " +
                $"'{definition.UserName}' als auch der bisherige Benutzer " +
                $"'{definition.LegacyUserName}' existieren bereits. " +
                "Die Benutzer können nicht automatisch zusammengeführt werden.");
        }

        if (user is not null)
        {
            return;
        }

        if (legacyUser is not null)
        {
            legacyUser.UserName =
                definition.UserName;

            legacyUser.DisplayName =
                definition.DisplayName;

            var migrationResult =
                await userManager.UpdateAsync(
                    legacyUser);

            EnsureSucceeded(
                migrationResult,
                $"Benutzer '{definition.LegacyUserName}' " +
                $"konnte nicht nach '{definition.UserName}' " +
                "migriert werden.");

            await AddRolesAsync(
                userManager,
                legacyUser,
                definition.Roles);

            return;
        }

        user =
            new ApplicationUser
            {
                UserName =
                    definition.UserName,

                DisplayName =
                    definition.DisplayName,

                IsActive =
                    true,

                EmailConfirmed =
                    true
            };

        var createResult =
            await userManager.CreateAsync(
                user,
                InitialPassword);

        EnsureSucceeded(
            createResult,
            $"Benutzer '{definition.UserName}' " +
            "konnte nicht angelegt werden.");

        await AddRolesAsync(
            userManager,
            user,
            definition.Roles);
    }

    private static async Task AddRolesAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationUser user,
        IReadOnlyCollection<string> roles)
    {
        var existingRoles =
            await userManager.GetRolesAsync(
                user);

        foreach (var role in roles)
        {
            if (existingRoles.Contains(
                role,
                StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            var result =
                await userManager.AddToRoleAsync(
                    user,
                    role);

            EnsureSucceeded(
                result,
                $"Rolle '{role}' konnte Benutzer " +
                $"'{user.UserName}' nicht zugewiesen werden.");
        }
    }

    private static async Task
        EnsureBootstrapAdministratorAsync(
            UserManager<ApplicationUser> userManager)
    {
        var administrators =
            await userManager.GetUsersInRoleAsync(
                UserAdministratorRole);

        if (administrators.Count > 0)
        {
            return;
        }

        var bootstrapAdministrator =
            await userManager.FindByNameAsync(
                BootstrapAdministratorUserName);

        if (bootstrapAdministrator is null)
        {
            throw new InvalidOperationException(
                $"Der Bootstrap-Administrator " +
                $"'{BootstrapAdministratorUserName}' wurde nicht gefunden.");
        }

        var result =
            await userManager.AddToRoleAsync(
                bootstrapAdministrator,
                UserAdministratorRole);

        EnsureSucceeded(
            result,
            $"Die Rolle '{UserAdministratorRole}' konnte dem " +
            $"Bootstrap-Administrator " +
            $"'{BootstrapAdministratorUserName}' nicht zugewiesen werden.");
    }

    private static void EnsureSucceeded(
        IdentityResult result,
        string message)
    {
        if (result.Succeeded)
        {
            return;
        }

        var errors =
            string.Join(
                "; ",
                result.Errors.Select(
                    error =>
                        error.Description));

        throw new InvalidOperationException(
            $"{message} {errors}");
    }

    private sealed record UserSeedDefinition(
        string? LegacyUserName,
        string UserName,
        string DisplayName,
        IReadOnlyCollection<string> Roles);
}
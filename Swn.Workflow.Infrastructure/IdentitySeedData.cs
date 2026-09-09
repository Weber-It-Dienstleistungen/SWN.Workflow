using Microsoft.AspNetCore.Identity;

namespace Swn.Workflow.Infrastructure;

public static class IdentitySeedData
{
    private const string DemoPassword = "Demo1234";

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
            "CONTROL_SYSTEM"
        };

        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var result =
                    await roleManager.CreateAsync(
                        new IdentityRole(roleName));

                EnsureSucceeded(
                    result,
                    $"Rolle '{roleName}' konnte nicht angelegt werden.");
            }
        }

        await CreateOrUpdateUserAsync(
            userManager,
            "hr.demo",
            "Personal Demo",
            new[]
            {
                "HR"
            });

        await CreateOrUpdateUserAsync(
            userManager,
            "it.demo",
            "IT Demo",
            new[]
            {
                "IT"
            });

        await CreateOrUpdateUserAsync(
            userManager,
            "organisation.demo",
            "Organisation Demo",
            new[]
            {
                "ORGANIZATION"
            });

        await CreateOrUpdateUserAsync(
            userManager,
            "telenec.demo",
            "Telenec Demo",
            new[]
            {
                "TELENEC"
            });

        await CreateOrUpdateUserAsync(
            userManager,
            "lager.demo",
            "Lager Demo",
            new[]
            {
                "WAREHOUSE"
            });

        await CreateOrUpdateUserAsync(
            userManager,
            "leitung.demo",
            "Leitung Demo",
            new[]
            {
                "SUPERVISOR",
                "EXECUTIVE_SECRETARIAT",
                "TECHNICAL_MANAGEMENT",
                "INFORMATION_SECURITY",
                "CONTROL_SYSTEM"
            });
    }

    private static async Task CreateOrUpdateUserAsync(
        UserManager<ApplicationUser> userManager,
        string userName,
        string displayName,
        IReadOnlyCollection<string> roles)
    {
        var user =
            await userManager.FindByNameAsync(userName);

        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = userName,
                DisplayName = displayName,
                EmailConfirmed = true
            };

            var createResult =
                await userManager.CreateAsync(
                    user,
                    DemoPassword);

            EnsureSucceeded(
                createResult,
                $"Benutzer '{userName}' konnte nicht angelegt werden.");
        }
        else if (user.DisplayName != displayName)
        {
            user.DisplayName = displayName;

            var updateResult =
                await userManager.UpdateAsync(user);

            EnsureSucceeded(
                updateResult,
                $"Benutzer '{userName}' konnte nicht aktualisiert werden.");
        }

        var existingRoles =
            await userManager.GetRolesAsync(user);

        foreach (var role in roles)
        {
            if (!existingRoles.Contains(
                role,
                StringComparer.OrdinalIgnoreCase))
            {
                var addRoleResult =
                    await userManager.AddToRoleAsync(
                        user,
                        role);

                EnsureSucceeded(
                    addRoleResult,
                    $"Rolle '{role}' konnte Benutzer '{userName}' nicht zugewiesen werden.");
            }
        }
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
                    error => error.Description));

        throw new InvalidOperationException(
            $"{message} {errors}");
    }
}
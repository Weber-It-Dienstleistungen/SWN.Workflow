using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Swn.Workflow.Application;

namespace Swn.Workflow.Infrastructure;

public sealed class UserAdministrationService
    : IUserAdministrationService
{
    private readonly UserManager<ApplicationUser>
        _userManager;

    private readonly RoleManager<IdentityRole>
        _roleManager;

    public UserAdministrationService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        ArgumentNullException.ThrowIfNull(
            userManager);

        ArgumentNullException.ThrowIfNull(
            roleManager);

        _userManager =
            userManager;

        _roleManager =
            roleManager;
    }

    public async Task<IReadOnlyList<UserAdministrationItem>>
        GetUsersAsync(
            CancellationToken cancellationToken = default)
    {
        var users =
            await _userManager.Users
                .ToListAsync(
                    cancellationToken);

        var result =
            new List<UserAdministrationItem>(
                users.Count);

        foreach (var user in users)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            var roles =
                await _userManager
                    .GetRolesAsync(
                        user);

            result.Add(
                new UserAdministrationItem(
                    user.Id,
                    user.UserName ??
                        string.Empty,
                    user.DisplayName,
                    user.Email,
                    user.IsActive,
                    roles
                        .OrderBy(
                            role => role,
                            StringComparer.OrdinalIgnoreCase)
                        .ToArray()));
        }

        return result
            .OrderBy(user =>
                string.IsNullOrWhiteSpace(
                    user.DisplayName)
                    ? user.UserName
                    : user.DisplayName,
                StringComparer.OrdinalIgnoreCase)
            .ThenBy(user =>
                user.UserName,
                StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public async Task<IReadOnlyList<string>>
        GetAvailableRolesAsync(
            CancellationToken cancellationToken = default)
    {
        return await _roleManager.Roles
            .AsNoTracking()
            .Where(role =>
                role.Name != null)
            .Select(role =>
                role.Name!)
            .OrderBy(
                role => role)
            .ToArrayAsync(
                cancellationToken);
    }

    public async Task<UserAdministrationOperationResult>
        CreateUserAsync(
            CreateUserRequest request,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        cancellationToken
            .ThrowIfCancellationRequested();

        var userName =
            request.UserName.Trim();

        var displayName =
            request.DisplayName.Trim();

        var email =
            request.Email.Trim();

        var password =
            request.Password;

        var roles =
            request.Roles
                .Where(role =>
                    !string.IsNullOrWhiteSpace(
                        role))
                .Select(role =>
                    role.Trim())
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .ToArray();

        var validationErrors =
            new List<string>();

        if (string.IsNullOrWhiteSpace(
            userName))
        {
            validationErrors.Add(
                "Bitte einen Benutzernamen angeben.");
        }

        if (string.IsNullOrWhiteSpace(
            displayName))
        {
            validationErrors.Add(
                "Bitte einen Anzeigenamen angeben.");
        }

        if (string.IsNullOrWhiteSpace(
            email))
        {
            validationErrors.Add(
                "Bitte eine E-Mail-Adresse angeben.");
        }
        else
        {
            var emailValidator =
                new EmailAddressAttribute();

            if (!emailValidator.IsValid(
                email))
            {
                validationErrors.Add(
                    "Bitte eine gültige E-Mail-Adresse angeben.");
            }
        }

        if (string.IsNullOrWhiteSpace(
            password))
        {
            validationErrors.Add(
                "Bitte ein Initialpasswort angeben.");
        }

        if (roles.Length == 0)
        {
            validationErrors.Add(
                "Bitte mindestens eine Rolle auswählen.");
        }

        if (validationErrors.Count > 0)
        {
            return new UserAdministrationOperationResult(
                false,
                validationErrors);
        }

        var existingUser =
            await _userManager.FindByNameAsync(
                userName);

        if (existingUser is not null)
        {
            return new UserAdministrationOperationResult(
                false,
                new[]
                {
                    $"Der Benutzername '{userName}' ist bereits vergeben."
                });
        }

        var availableRoles =
            await GetAvailableRolesAsync(
                cancellationToken);

        var invalidRoles =
            roles
                .Where(role =>
                    !availableRoles.Contains(
                        role,
                        StringComparer.OrdinalIgnoreCase))
                .ToArray();

        if (invalidRoles.Length > 0)
        {
            return new UserAdministrationOperationResult(
                false,
                new[]
                {
                    "Mindestens eine ausgewählte Rolle ist nicht vorhanden."
                });
        }

        var user =
            new ApplicationUser
            {
                UserName =
                    userName,

                DisplayName =
                    displayName,

                Email =
                    email,

                EmailConfirmed =
                    true,

                IsActive =
                    true
            };

        var createResult =
            await _userManager.CreateAsync(
                user,
                password);

        if (!createResult.Succeeded)
        {
            return new UserAdministrationOperationResult(
                false,
                GetErrors(
                    createResult));
        }

        var roleResult =
            await _userManager.AddToRolesAsync(
                user,
                roles);

        if (!roleResult.Succeeded)
        {
            var errors =
                GetErrors(
                    roleResult)
                .ToList();

            var deleteResult =
                await _userManager.DeleteAsync(
                    user);

            if (!deleteResult.Succeeded)
            {
                errors.Add(
                    "Der unvollständig angelegte Benutzer konnte " +
                    "nicht automatisch zurückgenommen werden.");

                errors.AddRange(
                    GetErrors(
                        deleteResult));
            }

            return new UserAdministrationOperationResult(
                false,
                errors);
        }

        return new UserAdministrationOperationResult(
            true,
            Array.Empty<string>());
    }

    private static IReadOnlyList<string> GetErrors(
        IdentityResult result)
    {
        return result.Errors
            .Select(error =>
                error.Description)
            .ToArray();
    }
}
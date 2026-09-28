using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Swn.Workflow.Application;

namespace Swn.Workflow.Infrastructure;

public sealed class UserAdministrationService
    : IUserAdministrationService
{
    private const string UserAdministratorRole =
        "USER_ADMIN";

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
            NormalizeRoles(
                request.Roles);

        var validationErrors =
            ValidateUserData(
                userName,
                displayName,
                email,
                roles);

        if (string.IsNullOrWhiteSpace(
            password))
        {
            validationErrors.Add(
                "Bitte ein Initialpasswort angeben.");
        }

        if (validationErrors.Count > 0)
        {
            return Failed(
                validationErrors);
        }

        var existingUser =
            await _userManager.FindByNameAsync(
                userName);

        if (existingUser is not null)
        {
            return Failed(
                $"Der Benutzername '{userName}' ist bereits vergeben.");
        }

        var roleValidationResult =
            await ValidateRolesAsync(
                roles,
                cancellationToken);

        if (!roleValidationResult.Succeeded)
        {
            return roleValidationResult;
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
            return Failed(
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

            return Failed(
                errors);
        }

        return Succeeded();
    }

    public async Task<UserAdministrationOperationResult>
        UpdateUserAsync(
            UpdateUserRequest request,
            string actingUserId,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        cancellationToken
            .ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(
            request.UserId))
        {
            return Failed(
                "Der zu bearbeitende Benutzer wurde nicht angegeben.");
        }

        if (string.IsNullOrWhiteSpace(
            actingUserId))
        {
            return Failed(
                "Der angemeldete Administrator konnte nicht ermittelt werden.");
        }

        var user =
            await _userManager.FindByIdAsync(
                request.UserId);

        if (user is null)
        {
            return Failed(
                "Der Benutzer wurde nicht gefunden.");
        }

        var userName =
            request.UserName.Trim();

        var displayName =
            request.DisplayName.Trim();

        var email =
            request.Email.Trim();

        var roles =
            NormalizeRoles(
                request.Roles);

        var validationErrors =
            ValidateUserData(
                userName,
                displayName,
                email,
                roles);

        if (validationErrors.Count > 0)
        {
            return Failed(
                validationErrors);
        }

        var userWithSameName =
            await _userManager.FindByNameAsync(
                userName);

        if (userWithSameName is not null &&
            !string.Equals(
                userWithSameName.Id,
                user.Id,
                StringComparison.Ordinal))
        {
            return Failed(
                $"Der Benutzername '{userName}' ist bereits vergeben.");
        }

        var roleValidationResult =
            await ValidateRolesAsync(
                roles,
                cancellationToken);

        if (!roleValidationResult.Succeeded)
        {
            return roleValidationResult;
        }

        if (string.Equals(
                user.Id,
                actingUserId,
                StringComparison.Ordinal) &&
            !request.IsActive)
        {
            return Failed(
                "Das eigene Benutzerkonto kann nicht deaktiviert werden.");
        }

        var existingRoles =
            await _userManager.GetRolesAsync(
                user);

        var currentlyIsAdministrator =
            existingRoles.Contains(
                UserAdministratorRole,
                StringComparer.OrdinalIgnoreCase);

        var remainsAdministrator =
            roles.Contains(
                UserAdministratorRole,
                StringComparer.OrdinalIgnoreCase);

        if (currentlyIsAdministrator &&
            (!request.IsActive ||
             !remainsAdministrator))
        {
            var otherActiveAdministrators =
                await GetOtherActiveAdministratorsAsync(
                    user.Id);

            if (otherActiveAdministrators == 0)
            {
                return Failed(
                    "Der letzte aktive Benutzeradministrator kann " +
                    "weder deaktiviert noch seiner Rolle USER_ADMIN " +
                    "beraubt werden.");
            }
        }

        user.UserName =
            userName;

        user.DisplayName =
            displayName;

        user.Email =
            email;

        user.EmailConfirmed =
            true;

        user.IsActive =
            request.IsActive;

        var updateResult =
            await _userManager.UpdateAsync(
                user);

        if (!updateResult.Succeeded)
        {
            return Failed(
                GetErrors(
                    updateResult));
        }

        var rolesToRemove =
            existingRoles
                .Where(existingRole =>
                    !roles.Contains(
                        existingRole,
                        StringComparer.OrdinalIgnoreCase))
                .ToArray();

        if (rolesToRemove.Length > 0)
        {
            var removeResult =
                await _userManager.RemoveFromRolesAsync(
                    user,
                    rolesToRemove);

            if (!removeResult.Succeeded)
            {
                return Failed(
                    GetErrors(
                        removeResult));
            }
        }

        var rolesAfterRemoval =
            await _userManager.GetRolesAsync(
                user);

        var rolesToAdd =
            roles
                .Where(role =>
                    !rolesAfterRemoval.Contains(
                        role,
                        StringComparer.OrdinalIgnoreCase))
                .ToArray();

        if (rolesToAdd.Length > 0)
        {
            var addResult =
                await _userManager.AddToRolesAsync(
                    user,
                    rolesToAdd);

            if (!addResult.Succeeded)
            {
                return Failed(
                    GetErrors(
                        addResult));
            }
        }

        return Succeeded();
    }

    private async Task<int>
        GetOtherActiveAdministratorsAsync(
            string excludedUserId)
    {
        var administrators =
            await _userManager.GetUsersInRoleAsync(
                UserAdministratorRole);

        return administrators.Count(user =>
            user.IsActive &&
            !string.Equals(
                user.Id,
                excludedUserId,
                StringComparison.Ordinal));
    }

    private async Task<UserAdministrationOperationResult>
        ValidateRolesAsync(
            IReadOnlyCollection<string> roles,
            CancellationToken cancellationToken)
    {
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
            return Failed(
                "Mindestens eine ausgewählte Rolle ist nicht vorhanden.");
        }

        return Succeeded();
    }

    private static string[] NormalizeRoles(
        IEnumerable<string> roles)
    {
        return roles
            .Where(role =>
                !string.IsNullOrWhiteSpace(
                    role))
            .Select(role =>
                role.Trim())
            .Distinct(
                StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static List<string> ValidateUserData(
        string userName,
        string displayName,
        string email,
        IReadOnlyCollection<string> roles)
    {
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

        if (roles.Count == 0)
        {
            validationErrors.Add(
                "Bitte mindestens eine Rolle auswählen.");
        }

        return validationErrors;
    }

    private static UserAdministrationOperationResult Succeeded()
    {
        return new UserAdministrationOperationResult(
            true,
            Array.Empty<string>());
    }

    private static UserAdministrationOperationResult Failed(
        string error)
    {
        return Failed(
            new[]
            {
                error
            });
    }

    private static UserAdministrationOperationResult Failed(
        IReadOnlyList<string> errors)
    {
        return new UserAdministrationOperationResult(
            false,
            errors);
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
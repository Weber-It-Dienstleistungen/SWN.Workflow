using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Swn.Workflow.Application;

namespace Swn.Workflow.Infrastructure;

public sealed class UserAdministrationService
    : IUserAdministrationService
{
    private readonly UserManager<ApplicationUser>
        _userManager;

    public UserAdministrationService(
        UserManager<ApplicationUser> userManager)
    {
        ArgumentNullException.ThrowIfNull(
            userManager);

        _userManager =
            userManager;
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
}
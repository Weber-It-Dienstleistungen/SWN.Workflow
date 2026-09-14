using Microsoft.AspNetCore.Identity;
using Swn.Workflow.Application;

namespace Swn.Workflow.Infrastructure;

public sealed class UserDirectoryService
    : IUserDirectoryService
{
    private readonly UserManager<ApplicationUser>
        _userManager;

    public UserDirectoryService(
        UserManager<ApplicationUser> userManager)
    {
        ArgumentNullException.ThrowIfNull(userManager);

        _userManager = userManager;
    }

    public async Task<IReadOnlyList<AssignableUserItem>>
        GetUsersInRoleAsync(
            string roleKey,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(roleKey);

        var normalizedRoleKey =
            roleKey.Trim();

        if (string.IsNullOrWhiteSpace(normalizedRoleKey))
        {
            throw new ArgumentException(
                "Role key must not be empty.",
                nameof(roleKey));
        }

        cancellationToken.ThrowIfCancellationRequested();

        var users =
            await _userManager.GetUsersInRoleAsync(
                normalizedRoleKey);

        cancellationToken.ThrowIfCancellationRequested();

        return users
            .Select(user =>
                new AssignableUserItem(
                    user.Id,
                    user.UserName ?? string.Empty,
                    user.DisplayName))
            .OrderBy(user =>
                string.IsNullOrWhiteSpace(user.DisplayName)
                    ? user.UserName
                    : user.DisplayName,
                StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
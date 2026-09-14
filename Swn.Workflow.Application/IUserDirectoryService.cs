namespace Swn.Workflow.Application;

public interface IUserDirectoryService
{
    Task<IReadOnlyList<AssignableUserItem>>
        GetUsersInRoleAsync(
            string roleKey,
            CancellationToken cancellationToken = default);
}

public sealed record AssignableUserItem(
    string UserId,
    string UserName,
    string DisplayName);
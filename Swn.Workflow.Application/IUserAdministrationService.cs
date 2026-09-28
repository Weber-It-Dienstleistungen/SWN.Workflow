namespace Swn.Workflow.Application;

public interface IUserAdministrationService
{
    Task<IReadOnlyList<UserAdministrationItem>> GetUsersAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetAvailableRolesAsync(
        CancellationToken cancellationToken = default);

    Task<UserAdministrationOperationResult> CreateUserAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default);

    Task<UserAdministrationOperationResult> UpdateUserAsync(
        UpdateUserRequest request,
        string actingUserId,
        CancellationToken cancellationToken = default);
}
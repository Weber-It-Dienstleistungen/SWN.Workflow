namespace Swn.Workflow.Application;

public interface IUserAdministrationService
{
    Task<IReadOnlyList<UserAdministrationItem>> GetUsersAsync(
        CancellationToken cancellationToken = default);
}
namespace Swn.Workflow.Application;

public sealed record CreateUserRequest(
    string UserName,
    string DisplayName,
    string Email,
    string Password,
    IReadOnlyCollection<string> Roles);
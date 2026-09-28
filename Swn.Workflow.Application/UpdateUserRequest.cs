namespace Swn.Workflow.Application;

public sealed record UpdateUserRequest(
    string UserId,
    string UserName,
    string DisplayName,
    string Email,
    bool IsActive,
    IReadOnlyCollection<string> Roles);
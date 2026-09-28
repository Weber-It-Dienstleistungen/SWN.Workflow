namespace Swn.Workflow.Application;

public sealed record UserAdministrationItem(
    string Id,
    string UserName,
    string DisplayName,
    string? Email,
    bool IsActive,
    IReadOnlyList<string> Roles);
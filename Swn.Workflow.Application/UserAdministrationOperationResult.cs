namespace Swn.Workflow.Application;

public sealed record UserAdministrationOperationResult(
    bool Succeeded,
    IReadOnlyList<string> Errors);
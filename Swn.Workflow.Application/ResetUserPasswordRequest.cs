namespace Swn.Workflow.Application;

public sealed record ResetUserPasswordRequest(
    string UserId,
    string NewPassword);
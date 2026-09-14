namespace Swn.Workflow.Domain;

public class WorkflowInstanceRoleAssignment
{
    public Guid Id { get; set; }

    public Guid WorkflowInstanceId { get; set; }

    public string RoleKey { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;
}
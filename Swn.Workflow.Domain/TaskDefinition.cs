namespace Swn.Workflow.Domain;

public class TaskDefinition
{
    public Guid Id { get; set; }

    public Guid WorkflowVersionId { get; set; }

    public string Key { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Phase { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public string AssignedRoleKey { get; set; } = string.Empty;

    public bool IsOptional { get; set; }
}
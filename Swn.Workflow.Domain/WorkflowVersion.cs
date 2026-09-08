namespace Swn.Workflow.Domain;

public class WorkflowVersion
{
    public Guid Id { get; set; }

    public Guid WorkflowDefinitionId { get; set; }

    public int VersionNumber { get; set; }

    public bool IsPublished { get; set; }
}
namespace Swn.Workflow.Domain;

public class WorkflowInstanceProperty
{
    public Guid Id { get; set; }

    public Guid WorkflowInstanceId { get; set; }

    public string Key { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;
}
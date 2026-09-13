namespace Swn.Workflow.Domain;

public class TaskDecisionOptionDefinition
{
    public Guid Id { get; set; }

    public Guid TaskDefinitionId { get; set; }

    public string Key { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}
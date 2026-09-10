namespace Swn.Workflow.Domain;

public class TaskFieldDefinition
{
    public Guid Id { get; set; }

    public Guid TaskDefinitionId { get; set; }

    public string Key { get; set; } =
        string.Empty;

    public string Label { get; set; } =
        string.Empty;

    public TaskFieldType FieldType { get; set; }

    public int SortOrder { get; set; }

    public bool IsRequired { get; set; }
}
namespace Swn.Workflow.Domain;

public class TaskInstanceFieldValue
{
    public Guid Id { get; set; }

    public Guid TaskInstanceId { get; set; }

    public string Key { get; set; } =
        string.Empty;

    public string Value { get; set; } =
        string.Empty;
}
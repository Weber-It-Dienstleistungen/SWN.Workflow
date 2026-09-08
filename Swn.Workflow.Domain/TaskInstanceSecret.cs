namespace Swn.Workflow.Domain;

public class TaskInstanceSecret
{
    public Guid Id { get; set; }

    public Guid TaskInstanceId { get; set; }

    public string Key { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public string EncryptedValue { get; set; } = string.Empty;
}
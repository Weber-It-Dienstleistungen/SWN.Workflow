namespace Swn.Workflow.Application;

public sealed record MyTaskFieldItem(
    string Key,
    string Label,
    int FieldType,
    int SortOrder,
    bool IsRequired,
    bool HasStoredValue,
    string? Value);
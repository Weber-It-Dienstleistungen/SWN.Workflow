namespace Swn.Workflow.Application;

public sealed class WorkflowTaskValidationException
    : Exception
{
    public WorkflowTaskValidationException(
        string message)
        : base(message)
    {
    }
}
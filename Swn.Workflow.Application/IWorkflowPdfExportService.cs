namespace Swn.Workflow.Application;

public interface IWorkflowPdfExportService
{
    Task<WorkflowPdfExportResult?> CreateForInitiatorAsync(
        Guid workflowInstanceId,
        string userId,
        CancellationToken cancellationToken = default);
}

public sealed record WorkflowPdfExportResult(
    byte[] Content,
    string FileName);
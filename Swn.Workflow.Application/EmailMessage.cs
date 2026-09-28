namespace Swn.Workflow.Application;

public sealed record EmailMessage(
    string RecipientAddress,
    string? RecipientName,
    string Subject,
    string TextBody,
    string? HtmlBody = null);
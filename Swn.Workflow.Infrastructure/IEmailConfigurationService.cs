namespace Swn.Workflow.Infrastructure;

public interface IEmailConfigurationService
{
    Task<EmailConfigurationView>
        GetAsync(
            CancellationToken cancellationToken = default);

    Task<EmailRuntimeConfiguration?>
        GetRuntimeAsync(
            CancellationToken cancellationToken = default);

    Task<EmailConfigurationOperationResult>
        SaveAsync(
            SaveEmailConfigurationRequest request,
            CancellationToken cancellationToken = default);
}

public sealed record EmailConfigurationView(
    bool Enabled,
    EmailTransportMode Transport,
    string SenderAddress,
    string SenderName,
    string Username,
    bool HasPassword,
    string SmtpHost,
    int SmtpPort,
    SmtpSecurityMode SmtpSecurity,
    string EwsMailboxAddress,
    bool UseAutodiscover,
    string EwsServiceUrl);

public sealed record EmailRuntimeConfiguration(
    bool Enabled,
    EmailTransportMode Transport,
    string SenderAddress,
    string SenderName,
    string Username,
    string Password,
    string SmtpHost,
    int SmtpPort,
    SmtpSecurityMode SmtpSecurity,
    string EwsMailboxAddress,
    bool UseAutodiscover,
    string EwsServiceUrl);

public sealed record SaveEmailConfigurationRequest(
    bool Enabled,
    EmailTransportMode Transport,
    string SenderAddress,
    string SenderName,
    string Username,
    string? Password,
    string SmtpHost,
    int SmtpPort,
    SmtpSecurityMode SmtpSecurity,
    string EwsMailboxAddress,
    bool UseAutodiscover,
    string EwsServiceUrl);

public sealed record EmailConfigurationOperationResult(
    bool Succeeded,
    IReadOnlyList<string> Errors);
namespace Swn.Workflow.Infrastructure;

public interface ISmtpConfigurationService
{
    Task<SmtpConfigurationView>
        GetAsync(
            CancellationToken cancellationToken = default);

    Task<SmtpConfigurationOperationResult>
        SaveAsync(
            SaveSmtpConfigurationRequest request,
            CancellationToken cancellationToken = default);
}

public sealed record SmtpConfigurationView(
    bool Enabled,
    string Host,
    int Port,
    SmtpSecurityMode Security,
    string FromAddress,
    string FromName,
    string Username,
    bool HasPassword);

public sealed record SaveSmtpConfigurationRequest(
    bool Enabled,
    string Host,
    int Port,
    SmtpSecurityMode Security,
    string FromAddress,
    string FromName,
    string Username,
    string? Password);

public sealed record SmtpConfigurationOperationResult(
    bool Succeeded,
    IReadOnlyList<string> Errors);
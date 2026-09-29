using Swn.Workflow.Application;

namespace Swn.Workflow.Infrastructure;

public sealed class ConfiguredEmailSender
    : IEmailSender
{
    private readonly IEmailConfigurationService
        _configurationService;

    private readonly SmtpEmailSender
        _smtpEmailSender;

    private readonly ExchangeEwsEmailSender
        _exchangeEwsEmailSender;

    public ConfiguredEmailSender(
        IEmailConfigurationService configurationService,
        SmtpEmailSender smtpEmailSender,
        ExchangeEwsEmailSender exchangeEwsEmailSender)
    {
        ArgumentNullException.ThrowIfNull(
            configurationService);

        ArgumentNullException.ThrowIfNull(
            smtpEmailSender);

        ArgumentNullException.ThrowIfNull(
            exchangeEwsEmailSender);

        _configurationService =
            configurationService;

        _smtpEmailSender =
            smtpEmailSender;

        _exchangeEwsEmailSender =
            exchangeEwsEmailSender;
    }

    public async Task SendAsync(
        EmailMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            message);

        var configuration =
            await _configurationService
                .GetRuntimeAsync(
                    cancellationToken);

        if (configuration is null)
        {
            throw new InvalidOperationException(
                "Es ist noch keine E-Mail-Konfiguration gespeichert.");
        }

        if (!configuration.Enabled)
        {
            throw new InvalidOperationException(
                "Der E-Mail-Versand ist nicht aktiviert.");
        }

        switch (configuration.Transport)
        {
            case EmailTransportMode.Smtp:
                await _smtpEmailSender
                    .SendAsync(
                        message,
                        cancellationToken);

                break;

            case EmailTransportMode.ExchangeEws:
                await _exchangeEwsEmailSender
                    .SendAsync(
                        message,
                        cancellationToken);

                break;

            default:
                throw new InvalidOperationException(
                    "Die konfigurierte E-Mail-Versandart " +
                    "wird nicht unterstützt.");
        }
    }
}
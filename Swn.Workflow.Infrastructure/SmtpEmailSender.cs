using MailKit.Security;
using MimeKit;
using Swn.Workflow.Application;

namespace Swn.Workflow.Infrastructure;

public sealed class SmtpEmailSender
    : IEmailSender
{
    private readonly IEmailConfigurationService
        _configurationService;

    public SmtpEmailSender(
        IEmailConfigurationService configurationService)
    {
        ArgumentNullException.ThrowIfNull(
            configurationService);

        _configurationService =
            configurationService;
    }

    public async Task SendAsync(
        EmailMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            message);

        ValidateMessage(
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

        ValidateConfiguration(
            configuration);

        var mimeMessage =
            new MimeMessage();

        mimeMessage.From.Add(
            new MailboxAddress(
                configuration.SenderName,
                configuration.SenderAddress));

        mimeMessage.To.Add(
            new MailboxAddress(
                string.IsNullOrWhiteSpace(
                    message.RecipientName)
                    ? message.RecipientAddress
                    : message.RecipientName,
                message.RecipientAddress));

        mimeMessage.Subject =
            message.Subject;

        var bodyBuilder =
            new BodyBuilder
            {
                TextBody =
                    message.TextBody
            };

        if (!string.IsNullOrWhiteSpace(
            message.HtmlBody))
        {
            bodyBuilder.HtmlBody =
                message.HtmlBody;
        }

        mimeMessage.Body =
            bodyBuilder.ToMessageBody();

        using var smtpClient =
            new MailKit.Net.Smtp.SmtpClient();

        await smtpClient.ConnectAsync(
            configuration.SmtpHost,
            configuration.SmtpPort,
            GetSecureSocketOptions(
                configuration.SmtpSecurity),
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(
            configuration.Username))
        {
            await smtpClient.AuthenticateAsync(
                configuration.Username,
                configuration.Password,
                cancellationToken);
        }

        await smtpClient.SendAsync(
            mimeMessage,
            cancellationToken);

        await smtpClient.DisconnectAsync(
            true,
            cancellationToken);
    }

    private static void ValidateMessage(
        EmailMessage message)
    {
        if (string.IsNullOrWhiteSpace(
            message.RecipientAddress))
        {
            throw new ArgumentException(
                "Die Empfängeradresse darf nicht leer sein.",
                nameof(message));
        }

        if (string.IsNullOrWhiteSpace(
            message.Subject))
        {
            throw new ArgumentException(
                "Der Betreff darf nicht leer sein.",
                nameof(message));
        }

        if (string.IsNullOrWhiteSpace(
                message.TextBody) &&
            string.IsNullOrWhiteSpace(
                message.HtmlBody))
        {
            throw new ArgumentException(
                "Die Nachricht muss einen Text- oder " +
                "HTML-Inhalt enthalten.",
                nameof(message));
        }
    }

    private static void ValidateConfiguration(
        EmailRuntimeConfiguration configuration)
    {
        if (!configuration.Enabled)
        {
            throw new InvalidOperationException(
                "Der E-Mail-Versand ist nicht aktiviert.");
        }

        if (configuration.Transport !=
            EmailTransportMode.Smtp)
        {
            throw new InvalidOperationException(
                "Die gespeicherte Versandart ist nicht SMTP.");
        }

        if (string.IsNullOrWhiteSpace(
            configuration.SmtpHost))
        {
            throw new InvalidOperationException(
                "Für den E-Mail-Versand ist kein " +
                "SMTP-Server konfiguriert.");
        }

        if (configuration.SmtpPort <= 0 ||
            configuration.SmtpPort > 65535)
        {
            throw new InvalidOperationException(
                "Der konfigurierte SMTP-Port ist ungültig.");
        }

        if (string.IsNullOrWhiteSpace(
            configuration.SenderAddress))
        {
            throw new InvalidOperationException(
                "Für den E-Mail-Versand ist keine " +
                "Absenderadresse konfiguriert.");
        }

        if (!string.IsNullOrWhiteSpace(
                configuration.Username) &&
            string.IsNullOrWhiteSpace(
                configuration.Password))
        {
            throw new InvalidOperationException(
                "Für den SMTP-Benutzernamen wurde kein " +
                "Passwort konfiguriert.");
        }
    }

    private static SecureSocketOptions
        GetSecureSocketOptions(
            SmtpSecurityMode security)
    {
        return security switch
        {
            SmtpSecurityMode.None =>
                SecureSocketOptions.None,

            SmtpSecurityMode.StartTls =>
                SecureSocketOptions.StartTls,

            SmtpSecurityMode.SslOnConnect =>
                SecureSocketOptions.SslOnConnect,

            _ =>
                throw new InvalidOperationException(
                    "Der konfigurierte SMTP-Sicherheitsmodus " +
                    "ist ungültig.")
        };
    }
}
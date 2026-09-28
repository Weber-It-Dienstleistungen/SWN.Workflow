using MailKit.Security;
using MimeKit;
using Swn.Workflow.Application;

namespace Swn.Workflow.Infrastructure;

public sealed class SmtpEmailSender
    : IEmailSender
{
    private readonly SmtpSettings
        _settings;

    public SmtpEmailSender(
        SmtpSettings settings)
    {
        ArgumentNullException.ThrowIfNull(
            settings);

        _settings =
            settings;
    }

    public async Task SendAsync(
        EmailMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            message);

        ValidateConfiguration();

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
                "Die Nachricht muss einen Text- oder HTML-Inhalt enthalten.",
                nameof(message));
        }

        var mimeMessage =
            new MimeMessage();

        mimeMessage.From.Add(
            new MailboxAddress(
                _settings.FromName,
                _settings.FromAddress));

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
            _settings.Host,
            _settings.Port,
            GetSecureSocketOptions(),
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(
            _settings.Username))
        {
            await smtpClient.AuthenticateAsync(
                _settings.Username,
                _settings.Password,
                cancellationToken);
        }

        await smtpClient.SendAsync(
            mimeMessage,
            cancellationToken);

        await smtpClient.DisconnectAsync(
            true,
            cancellationToken);
    }

    private void ValidateConfiguration()
    {
        if (!_settings.Enabled)
        {
            throw new InvalidOperationException(
                "Der E-Mail-Versand ist nicht aktiviert.");
        }

        if (string.IsNullOrWhiteSpace(
            _settings.Host))
        {
            throw new InvalidOperationException(
                "Für den E-Mail-Versand ist kein SMTP-Server konfiguriert.");
        }

        if (_settings.Port <= 0 ||
            _settings.Port > 65535)
        {
            throw new InvalidOperationException(
                "Der konfigurierte SMTP-Port ist ungültig.");
        }

        if (string.IsNullOrWhiteSpace(
            _settings.FromAddress))
        {
            throw new InvalidOperationException(
                "Für den E-Mail-Versand ist keine Absenderadresse konfiguriert.");
        }

        if (!string.IsNullOrWhiteSpace(
                _settings.Username) &&
            string.IsNullOrWhiteSpace(
                _settings.Password))
        {
            throw new InvalidOperationException(
                "Für den SMTP-Benutzernamen wurde kein Passwort konfiguriert.");
        }
    }

    private SecureSocketOptions GetSecureSocketOptions()
    {
        return _settings.Security switch
        {
            SmtpSecurityMode.None =>
                SecureSocketOptions.None,

            SmtpSecurityMode.StartTls =>
                SecureSocketOptions.StartTls,

            SmtpSecurityMode.SslOnConnect =>
                SecureSocketOptions.SslOnConnect,

            _ =>
                throw new InvalidOperationException(
                    "Der konfigurierte SMTP-Sicherheitsmodus ist ungültig.")
        };
    }
}
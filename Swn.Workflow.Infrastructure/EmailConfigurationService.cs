using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace Swn.Workflow.Infrastructure;

public sealed class EmailConfigurationService
    : IEmailConfigurationService
{
    private const int ConfigurationId =
        1;

    /*
     * Diesen Purpose bewusst nicht umbenennen.
     * Damit bleiben bereits mit der bisherigen
     * SMTP-Konfiguration verschlüsselte Passwörter
     * weiterhin kompatibel.
     */
    private const string PasswordProtectionPurpose =
        "Swn.Workflow.SmtpConfiguration.Password.v1";

    private readonly IDbContextFactory<WorkflowDbContext>
        _dbContextFactory;

    private readonly IDataProtector
        _passwordProtector;

    public EmailConfigurationService(
        IDbContextFactory<WorkflowDbContext> dbContextFactory,
        IDataProtectionProvider dataProtectionProvider)
    {
        ArgumentNullException.ThrowIfNull(
            dbContextFactory);

        ArgumentNullException.ThrowIfNull(
            dataProtectionProvider);

        _dbContextFactory =
            dbContextFactory;

        _passwordProtector =
            dataProtectionProvider.CreateProtector(
                PasswordProtectionPurpose);
    }

    public async Task<EmailConfigurationView>
        GetAsync(
            CancellationToken cancellationToken = default)
    {
        await using var db =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        var configuration =
            await db.EmailConfigurations
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item =>
                        item.Id ==
                        ConfigurationId,
                    cancellationToken);

        if (configuration is null)
        {
            return CreateDefaultView();
        }

        return new EmailConfigurationView(
            configuration.Enabled,
            configuration.Transport,
            configuration.SenderAddress,
            configuration.SenderName,
            configuration.Username,
            !string.IsNullOrWhiteSpace(
                configuration.EncryptedPassword),
            configuration.SmtpHost,
            configuration.SmtpPort,
            configuration.SmtpSecurity,
            configuration.EwsMailboxAddress,
            configuration.UseAutodiscover,
            configuration.EwsServiceUrl);
    }

    public async Task<EmailRuntimeConfiguration?>
        GetRuntimeAsync(
            CancellationToken cancellationToken = default)
    {
        await using var db =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        var configuration =
            await db.EmailConfigurations
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item =>
                        item.Id ==
                        ConfigurationId,
                    cancellationToken);

        if (configuration is null)
        {
            return null;
        }

        var password =
            string.Empty;

        if (!string.IsNullOrWhiteSpace(
            configuration.EncryptedPassword))
        {
            try
            {
                password =
                    _passwordProtector.Unprotect(
                        configuration.EncryptedPassword);
            }
            catch (CryptographicException exception)
            {
                throw new InvalidOperationException(
                    "Das gespeicherte E-Mail-Passwort konnte " +
                    "nicht entschlüsselt werden.",
                    exception);
            }
        }

        return new EmailRuntimeConfiguration(
            configuration.Enabled,
            configuration.Transport,
            configuration.SenderAddress,
            configuration.SenderName,
            configuration.Username,
            password,
            configuration.SmtpHost,
            configuration.SmtpPort,
            configuration.SmtpSecurity,
            configuration.EwsMailboxAddress,
            configuration.UseAutodiscover,
            configuration.EwsServiceUrl);
    }

    public async Task<EmailConfigurationOperationResult>
        SaveAsync(
            SaveEmailConfigurationRequest request,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        cancellationToken
            .ThrowIfCancellationRequested();

        var senderAddress =
            request.SenderAddress?.Trim()
            ?? string.Empty;

        var senderName =
            request.SenderName?.Trim()
            ?? string.Empty;

        var username =
            request.Username?.Trim()
            ?? string.Empty;

        var smtpHost =
            request.SmtpHost?.Trim()
            ?? string.Empty;

        var ewsMailboxAddress =
            request.EwsMailboxAddress?.Trim()
            ?? string.Empty;

        var ewsServiceUrl =
            request.EwsServiceUrl?.Trim()
            ?? string.Empty;

        await using var db =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        var configuration =
            await db.EmailConfigurations
                .SingleOrDefaultAsync(
                    item =>
                        item.Id ==
                        ConfigurationId,
                    cancellationToken);

        var hasExistingPassword =
            !string.IsNullOrWhiteSpace(
                configuration?.EncryptedPassword);

        var hasNewPassword =
            !string.IsNullOrEmpty(
                request.Password);

        var validationErrors =
            Validate(
                request.Enabled,
                request.Transport,
                senderAddress,
                senderName,
                username,
                hasExistingPassword ||
                hasNewPassword,
                hasNewPassword,
                smtpHost,
                request.SmtpPort,
                request.SmtpSecurity,
                ewsMailboxAddress,
                request.UseAutodiscover,
                ewsServiceUrl);

        if (validationErrors.Count > 0)
        {
            return Failed(
                validationErrors);
        }

        if (configuration is null)
        {
            configuration =
                new EmailConfiguration
                {
                    Id =
                        ConfigurationId
                };

            db.EmailConfigurations.Add(
                configuration);
        }

        configuration.Enabled =
            request.Enabled;

        configuration.Transport =
            request.Transport;

        configuration.SenderAddress =
            senderAddress;

        configuration.SenderName =
            senderName;

        configuration.Username =
            username;

        configuration.SmtpHost =
            smtpHost;

        configuration.SmtpPort =
            request.SmtpPort;

        configuration.SmtpSecurity =
            request.SmtpSecurity;

        configuration.EwsMailboxAddress =
            ewsMailboxAddress;

        configuration.UseAutodiscover =
            request.UseAutodiscover;

        configuration.EwsServiceUrl =
            ewsServiceUrl;

        if (string.IsNullOrWhiteSpace(
            username))
        {
            configuration.EncryptedPassword =
                null;
        }
        else if (hasNewPassword)
        {
            configuration.EncryptedPassword =
                _passwordProtector.Protect(
                    request.Password!);
        }

        await db.SaveChangesAsync(
            cancellationToken);

        return Succeeded();
    }

    private static List<string> Validate(
        bool enabled,
        EmailTransportMode transport,
        string senderAddress,
        string senderName,
        string username,
        bool hasPassword,
        bool hasNewPassword,
        string smtpHost,
        int smtpPort,
        SmtpSecurityMode smtpSecurity,
        string ewsMailboxAddress,
        bool useAutodiscover,
        string ewsServiceUrl)
    {
        var errors =
            new List<string>();

        if (!Enum.IsDefined(
            typeof(EmailTransportMode),
            transport))
        {
            errors.Add(
                "Die ausgewählte Versandart ist ungültig.");
        }

        if (senderAddress.Length > 256)
        {
            errors.Add(
                "Die Absenderadresse darf maximal 256 Zeichen lang sein.");
        }

        if (senderName.Length > 200)
        {
            errors.Add(
                "Der Absendername darf maximal 200 Zeichen lang sein.");
        }

        if (username.Length > 256)
        {
            errors.Add(
                "Der Benutzername darf maximal 256 Zeichen lang sein.");
        }

        if (smtpHost.Length > 255)
        {
            errors.Add(
                "Der SMTP-Server darf maximal 255 Zeichen lang sein.");
        }

        if (ewsMailboxAddress.Length > 256)
        {
            errors.Add(
                "Die Exchange-Postfachadresse darf maximal 256 Zeichen lang sein.");
        }

        if (ewsServiceUrl.Length > 2000)
        {
            errors.Add(
                "Der EWS-Endpunkt darf maximal 2000 Zeichen lang sein.");
        }

        ValidateEmailAddress(
            senderAddress,
            "Bitte eine gültige Absenderadresse angeben.",
            errors);

        ValidateEmailAddress(
            ewsMailboxAddress,
            "Bitte eine gültige Exchange-Postfachadresse angeben.",
            errors);

        if (!string.IsNullOrWhiteSpace(
            ewsServiceUrl))
        {
            if (!Uri.TryCreate(
                    ewsServiceUrl,
                    UriKind.Absolute,
                    out var ewsUri) ||
                !string.Equals(
                    ewsUri.Scheme,
                    Uri.UriSchemeHttps,
                    StringComparison.OrdinalIgnoreCase))
            {
                errors.Add(
                    "Der manuelle EWS-Endpunkt muss eine gültige HTTPS-Adresse sein.");
            }
        }

        if (hasNewPassword &&
            string.IsNullOrWhiteSpace(
                username))
        {
            errors.Add(
                "Ein Passwort kann nur zusammen mit einem Benutzernamen gespeichert werden.");
        }

        if (!enabled)
        {
            return errors;
        }

        if (string.IsNullOrWhiteSpace(
            senderAddress))
        {
            errors.Add(
                "Für den aktivierten E-Mail-Versand muss eine Absenderadresse angegeben werden.");
        }

        if (string.IsNullOrWhiteSpace(
            senderName))
        {
            errors.Add(
                "Für den aktivierten E-Mail-Versand muss ein Absendername angegeben werden.");
        }

        switch (transport)
        {
            case EmailTransportMode.Smtp:
                ValidateSmtp(
                    smtpHost,
                    smtpPort,
                    smtpSecurity,
                    username,
                    hasPassword,
                    errors);

                break;

            case EmailTransportMode.ExchangeEws:
                ValidateExchangeEws(
                    ewsMailboxAddress,
                    username,
                    hasPassword,
                    useAutodiscover,
                    ewsServiceUrl,
                    errors);

                break;
        }

        return errors;
    }

    private static void ValidateSmtp(
        string smtpHost,
        int smtpPort,
        SmtpSecurityMode smtpSecurity,
        string username,
        bool hasPassword,
        ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(
            smtpHost))
        {
            errors.Add(
                "Für SMTP muss ein SMTP-Server angegeben werden.");
        }

        if (smtpPort <= 0 ||
            smtpPort > 65535)
        {
            errors.Add(
                "Der SMTP-Port muss zwischen 1 und 65535 liegen.");
        }

        if (!Enum.IsDefined(
            typeof(SmtpSecurityMode),
            smtpSecurity))
        {
            errors.Add(
                "Der ausgewählte SMTP-Sicherheitsmodus ist ungültig.");
        }

        if (!string.IsNullOrWhiteSpace(
                username) &&
            !hasPassword)
        {
            errors.Add(
                "Für den angegebenen SMTP-Benutzernamen muss ein Passwort hinterlegt werden.");
        }
    }

    private static void ValidateExchangeEws(
        string ewsMailboxAddress,
        string username,
        bool hasPassword,
        bool useAutodiscover,
        string ewsServiceUrl,
        ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(
            ewsMailboxAddress))
        {
            errors.Add(
                "Für Exchange muss eine Postfachadresse angegeben werden.");
        }

        if (string.IsNullOrWhiteSpace(
            username))
        {
            errors.Add(
                "Für Exchange muss ein Benutzername angegeben werden.");
        }

        if (!hasPassword)
        {
            errors.Add(
                "Für Exchange muss ein Passwort hinterlegt werden.");
        }

        if (!useAutodiscover &&
            string.IsNullOrWhiteSpace(
                ewsServiceUrl))
        {
            errors.Add(
                "Wenn Autodiscover deaktiviert ist, muss ein manueller EWS-Endpunkt angegeben werden.");
        }
    }

    private static void ValidateEmailAddress(
        string address,
        string errorMessage,
        ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(
            address))
        {
            return;
        }

        var validator =
            new EmailAddressAttribute();

        if (!validator.IsValid(
            address))
        {
            errors.Add(
                errorMessage);
        }
    }

    private static EmailConfigurationView
        CreateDefaultView()
    {
        return new EmailConfigurationView(
            Enabled: false,
            Transport:
                EmailTransportMode.ExchangeEws,
            SenderAddress:
                string.Empty,
            SenderName:
                "SWN Workflow",
            Username:
                string.Empty,
            HasPassword:
                false,
            SmtpHost:
                string.Empty,
            SmtpPort:
                587,
            SmtpSecurity:
                SmtpSecurityMode.StartTls,
            EwsMailboxAddress:
                string.Empty,
            UseAutodiscover:
                true,
            EwsServiceUrl:
                string.Empty);
    }

    private static EmailConfigurationOperationResult
        Succeeded()
    {
        return new EmailConfigurationOperationResult(
            true,
            Array.Empty<string>());
    }

    private static EmailConfigurationOperationResult
        Failed(
            IReadOnlyList<string> errors)
    {
        return new EmailConfigurationOperationResult(
            false,
            errors);
    }
}
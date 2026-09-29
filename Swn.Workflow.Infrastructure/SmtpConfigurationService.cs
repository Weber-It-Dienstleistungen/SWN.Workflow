using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace Swn.Workflow.Infrastructure;

public sealed class SmtpConfigurationService
    : ISmtpConfigurationService
{
    private const int ConfigurationId =
        1;

    private const string PasswordProtectionPurpose =
        "Swn.Workflow.SmtpConfiguration.Password.v1";

    private readonly IDbContextFactory<WorkflowDbContext>
        _dbContextFactory;

    private readonly IDataProtector
        _passwordProtector;

    public SmtpConfigurationService(
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

    public async Task<SmtpConfigurationView>
        GetAsync(
            CancellationToken cancellationToken = default)
    {
        await using var db =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        var configuration =
            await db.SmtpConfigurations
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

        return new SmtpConfigurationView(
            configuration.Enabled,
            configuration.Host,
            configuration.Port,
            configuration.Security,
            configuration.FromAddress,
            configuration.FromName,
            configuration.Username,
            !string.IsNullOrWhiteSpace(
                configuration.EncryptedPassword));
    }

    public async Task<SmtpConfigurationOperationResult>
        SaveAsync(
            SaveSmtpConfigurationRequest request,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        cancellationToken
            .ThrowIfCancellationRequested();

        var host =
            request.Host.Trim();

        var fromAddress =
            request.FromAddress.Trim();

        var fromName =
            request.FromName.Trim();

        var username =
            request.Username.Trim();

        await using var db =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        var configuration =
            await db.SmtpConfigurations
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
                host,
                request.Port,
                request.Security,
                fromAddress,
                fromName,
                username,
                hasExistingPassword ||
                hasNewPassword,
                hasNewPassword);

        if (validationErrors.Count > 0)
        {
            return Failed(
                validationErrors);
        }

        if (configuration is null)
        {
            configuration =
                new SmtpConfiguration
                {
                    Id =
                        ConfigurationId
                };

            db.SmtpConfigurations.Add(
                configuration);
        }

        configuration.Enabled =
            request.Enabled;

        configuration.Host =
            host;

        configuration.Port =
            request.Port;

        configuration.Security =
            request.Security;

        configuration.FromAddress =
            fromAddress;

        configuration.FromName =
            fromName;

        configuration.Username =
            username;

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
        string host,
        int port,
        SmtpSecurityMode security,
        string fromAddress,
        string fromName,
        string username,
        bool hasPassword,
        bool hasNewPassword)
    {
        var errors =
            new List<string>();

        if (port <= 0 ||
            port > 65535)
        {
            errors.Add(
                "Der SMTP-Port muss zwischen 1 und 65535 liegen.");
        }

        if (!Enum.IsDefined(
            typeof(SmtpSecurityMode),
            security))
        {
            errors.Add(
                "Der ausgewählte SMTP-Sicherheitsmodus ist ungültig.");
        }

        if (host.Length > 255)
        {
            errors.Add(
                "Der SMTP-Server darf maximal 255 Zeichen lang sein.");
        }

        if (fromAddress.Length > 256)
        {
            errors.Add(
                "Die Absenderadresse darf maximal 256 Zeichen lang sein.");
        }

        if (fromName.Length > 200)
        {
            errors.Add(
                "Der Absendername darf maximal 200 Zeichen lang sein.");
        }

        if (username.Length > 256)
        {
            errors.Add(
                "Der SMTP-Benutzername darf maximal 256 Zeichen lang sein.");
        }

        if (!string.IsNullOrWhiteSpace(
            fromAddress))
        {
            var emailValidator =
                new EmailAddressAttribute();

            if (!emailValidator.IsValid(
                fromAddress))
            {
                errors.Add(
                    "Bitte eine gültige Absenderadresse angeben.");
            }
        }

        if (hasNewPassword &&
            string.IsNullOrWhiteSpace(
                username))
        {
            errors.Add(
                "Ein SMTP-Passwort kann nur zusammen mit einem " +
                "SMTP-Benutzernamen gespeichert werden.");
        }

        if (!enabled)
        {
            return errors;
        }

        if (string.IsNullOrWhiteSpace(
            host))
        {
            errors.Add(
                "Für den aktivierten E-Mail-Versand muss ein " +
                "SMTP-Server angegeben werden.");
        }

        if (string.IsNullOrWhiteSpace(
            fromAddress))
        {
            errors.Add(
                "Für den aktivierten E-Mail-Versand muss eine " +
                "Absenderadresse angegeben werden.");
        }

        if (string.IsNullOrWhiteSpace(
            fromName))
        {
            errors.Add(
                "Für den aktivierten E-Mail-Versand muss ein " +
                "Absendername angegeben werden.");
        }

        if (!string.IsNullOrWhiteSpace(
                username) &&
            !hasPassword)
        {
            errors.Add(
                "Für den angegebenen SMTP-Benutzernamen muss ein " +
                "Passwort hinterlegt werden.");
        }

        return errors;
    }

    private static SmtpConfigurationView
        CreateDefaultView()
    {
        return new SmtpConfigurationView(
            Enabled: false,
            Host: string.Empty,
            Port: 587,
            Security:
                SmtpSecurityMode.StartTls,
            FromAddress: string.Empty,
            FromName: "SWN Workflow",
            Username: string.Empty,
            HasPassword: false);
    }

    private static SmtpConfigurationOperationResult
        Succeeded()
    {
        return new SmtpConfigurationOperationResult(
            true,
            Array.Empty<string>());
    }

    private static SmtpConfigurationOperationResult
        Failed(
            IReadOnlyList<string> errors)
    {
        return new SmtpConfigurationOperationResult(
            false,
            errors);
    }
}
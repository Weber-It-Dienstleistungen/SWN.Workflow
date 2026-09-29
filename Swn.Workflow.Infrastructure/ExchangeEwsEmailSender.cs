using System.Net;
using System.Text;
using System.Xml.Linq;
using Swn.Workflow.Application;

namespace Swn.Workflow.Infrastructure;

public sealed class ExchangeEwsEmailSender
    : IEmailSender
{
    private readonly IEmailConfigurationService
        _configurationService;

    private readonly IExchangeEwsConnectionTester
        _connectionTester;

    public ExchangeEwsEmailSender(
        IEmailConfigurationService configurationService,
        IExchangeEwsConnectionTester connectionTester)
    {
        ArgumentNullException.ThrowIfNull(
            configurationService);

        ArgumentNullException.ThrowIfNull(
            connectionTester);

        _configurationService =
            configurationService;

        _connectionTester =
            connectionTester;
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

        var connectionResult =
            await _connectionTester
                .TestAsync(
                    cancellationToken);

        if (!connectionResult.Succeeded)
        {
            throw new InvalidOperationException(
                "Der EWS-Verbindungstest ist fehlgeschlagen: " +
                connectionResult.Message);
        }

        if (!TryCreateHttpsUri(
            connectionResult.EwsServiceUrl,
            out var ewsServiceUri))
        {
            throw new InvalidOperationException(
                "Der EWS-Verbindungstest lieferte keinen " +
                "gültigen HTTPS-Endpunkt.");
        }

        var credential =
            CreateCredential(
                configuration.Username,
                configuration.Password);

        using var httpClient =
            CreateHttpClient(
                credential);

        using var request =
            new HttpRequestMessage(
                HttpMethod.Post,
                ewsServiceUri);

        request.Headers.TryAddWithoutValidation(
            "SOAPAction",
            "http://schemas.microsoft.com/exchange/" +
            "services/2006/messages/CreateItem");

        request.Content =
            new StringContent(
                CreateSendRequest(
                    message),
                Encoding.UTF8,
                "text/xml");

        using var response =
            await httpClient.SendAsync(
                request,
                cancellationToken);

        if (response.StatusCode ==
            HttpStatusCode.Unauthorized)
        {
            throw new InvalidOperationException(
                "Der EWS-Server hat die Zugangsdaten " +
                "beim Versand abgelehnt (HTTP 401).");
        }

        if (response.StatusCode ==
            HttpStatusCode.Forbidden)
        {
            throw new InvalidOperationException(
                "Der EWS-Server hat den Versand " +
                "verweigert (HTTP 403).");
        }

        var responseText =
            await response.Content
                .ReadAsStringAsync(
                    cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Der EWS-Server antwortete beim Versand mit HTTP " +
                $"{(int)response.StatusCode} {response.ReasonPhrase}.");
        }

        XDocument document;

        try
        {
            document =
                XDocument.Parse(
                    responseText);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                "Der EWS-Server antwortete beim Versand, " +
                "lieferte aber keine gültige EWS-XML-Antwort.",
                exception);
        }

        var responseCode =
            FindElementValue(
                document,
                "ResponseCode");

        if (!string.Equals(
            responseCode,
            "NoError",
            StringComparison.OrdinalIgnoreCase))
        {
            var messageText =
                FindElementValue(
                    document,
                    "MessageText");

            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(
                    messageText)
                    ? $"EWS meldete beim Versand den Fehler " +
                      $"'{responseCode ?? "unbekannt"}'."
                    : $"EWS meldete beim Versand " +
                      $"'{responseCode ?? "unbekannt"}': " +
                      messageText);
        }
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
            EmailTransportMode.ExchangeEws)
        {
            throw new InvalidOperationException(
                "Die gespeicherte Versandart ist nicht Exchange (EWS).");
        }

        if (string.IsNullOrWhiteSpace(
            configuration.EwsMailboxAddress))
        {
            throw new InvalidOperationException(
                "Es ist keine Exchange-Postfachadresse hinterlegt.");
        }

        if (string.IsNullOrWhiteSpace(
            configuration.Username))
        {
            throw new InvalidOperationException(
                "Es ist kein Exchange-Benutzername hinterlegt.");
        }

        if (string.IsNullOrWhiteSpace(
            configuration.Password))
        {
            throw new InvalidOperationException(
                "Es ist kein Exchange-Passwort hinterlegt.");
        }
    }

    private static HttpClient CreateHttpClient(
        NetworkCredential credential)
    {
        var handler =
            new HttpClientHandler
            {
                AllowAutoRedirect =
                    false,

                Credentials =
                    credential,

                PreAuthenticate =
                    true,

                UseCookies =
                    false,

                AutomaticDecompression =
                    DecompressionMethods.GZip |
                    DecompressionMethods.Deflate
            };

        return new HttpClient(
            handler,
            disposeHandler: true)
        {
            Timeout =
                TimeSpan.FromSeconds(
                    20)
        };
    }

    private static NetworkCredential CreateCredential(
        string username,
        string password)
    {
        var separatorIndex =
            username.IndexOf(
                '\\');

        if (separatorIndex > 0 &&
            separatorIndex <
            username.Length - 1)
        {
            var domain =
                username[..separatorIndex];

            var user =
                username[(separatorIndex + 1)..];

            return new NetworkCredential(
                user,
                password,
                domain);
        }

        return new NetworkCredential(
            username,
            password);
    }

    private static string CreateSendRequest(
        EmailMessage message)
    {
        XNamespace soap =
            "http://schemas.xmlsoap.org/soap/envelope/";

        XNamespace messages =
            "http://schemas.microsoft.com/exchange/services/2006/messages";

        XNamespace types =
            "http://schemas.microsoft.com/exchange/services/2006/types";

        var bodyType =
            string.IsNullOrWhiteSpace(
                message.HtmlBody)
                ? "Text"
                : "HTML";

        var body =
            string.IsNullOrWhiteSpace(
                message.HtmlBody)
                ? message.TextBody
                : message.HtmlBody!;

        var recipientMailbox =
            new XElement(
                types + "Mailbox");

        if (!string.IsNullOrWhiteSpace(
            message.RecipientName))
        {
            recipientMailbox.Add(
                new XElement(
                    types + "Name",
                    message.RecipientName));
        }

        recipientMailbox.Add(
            new XElement(
                types + "EmailAddress",
                message.RecipientAddress));

        var document =
            new XDocument(
                new XElement(
                    soap + "Envelope",

                    new XAttribute(
                        XNamespace.Xmlns + "soap",
                        soap),

                    new XAttribute(
                        XNamespace.Xmlns + "m",
                        messages),

                    new XAttribute(
                        XNamespace.Xmlns + "t",
                        types),

                    new XElement(
                        soap + "Header",

                        new XElement(
                            types + "RequestServerVersion",

                            new XAttribute(
                                "Version",
                                "Exchange2013"))),

                    new XElement(
                        soap + "Body",

                        new XElement(
                            messages + "CreateItem",

                            new XAttribute(
                                "MessageDisposition",
                                "SendAndSaveCopy"),

                            new XElement(
                                messages + "SavedItemFolderId",

                                new XElement(
                                    types + "DistinguishedFolderId",

                                    new XAttribute(
                                        "Id",
                                        "sentitems"))),

                            new XElement(
                                messages + "Items",

                                new XElement(
                                    types + "Message",

                                    new XElement(
                                        types + "Subject",
                                        message.Subject),

                                    new XElement(
                                        types + "Body",

                                        new XAttribute(
                                            "BodyType",
                                            bodyType),

                                        body),

                                    new XElement(
                                        types + "ToRecipients",
                                        recipientMailbox)))))));

        return document.ToString(
            SaveOptions.DisableFormatting);
    }

    private static string? FindElementValue(
        XDocument document,
        string localName)
    {
        return document
            .Descendants()
            .FirstOrDefault(element =>
                string.Equals(
                    element.Name.LocalName,
                    localName,
                    StringComparison.OrdinalIgnoreCase))?
            .Value
            .Trim();
    }

    private static bool TryCreateHttpsUri(
        string? value,
        out Uri uri)
    {
        if (Uri.TryCreate(
                value,
                UriKind.Absolute,
                out var parsedUri) &&
            string.Equals(
                parsedUri.Scheme,
                Uri.UriSchemeHttps,
                StringComparison.OrdinalIgnoreCase))
        {
            uri =
                parsedUri;

            return true;
        }

        uri =
            null!;

        return false;
    }
}
using System.Net;
using System.Text;
using System.Xml.Linq;

namespace Swn.Workflow.Infrastructure;

public sealed class ExchangeEwsConnectionTester
    : IExchangeEwsConnectionTester
{
    private const int MaxAutodiscoverRedirects =
        5;

    private readonly IEmailConfigurationService
        _configurationService;

    public ExchangeEwsConnectionTester(
        IEmailConfigurationService configurationService)
    {
        ArgumentNullException.ThrowIfNull(
            configurationService);

        _configurationService =
            configurationService;
    }

    public async Task<ExchangeEwsConnectionTestResult>
        TestAsync(
            CancellationToken cancellationToken = default)
    {
        var configuration =
            await _configurationService
                .GetRuntimeAsync(
                    cancellationToken);

        if (configuration is null)
        {
            return Failed(
                "Es ist noch keine E-Mail-Konfiguration gespeichert.");
        }

        if (configuration.Transport !=
            EmailTransportMode.ExchangeEws)
        {
            return Failed(
                "Die gespeicherte Versandart ist nicht Exchange (EWS).");
        }

        if (string.IsNullOrWhiteSpace(
            configuration.EwsMailboxAddress))
        {
            return Failed(
                "Es ist keine Exchange-Postfachadresse hinterlegt.");
        }

        if (string.IsNullOrWhiteSpace(
            configuration.Username))
        {
            return Failed(
                "Es ist kein Exchange-Benutzername hinterlegt.");
        }

        if (string.IsNullOrWhiteSpace(
            configuration.Password))
        {
            return Failed(
                "Es ist kein Exchange-Passwort hinterlegt.");
        }

        var credential =
            CreateCredential(
                configuration.Username,
                configuration.Password);

        Uri ewsServiceUri;

        if (configuration.UseAutodiscover)
        {
            var autodiscoverResult =
                await DiscoverEwsServiceAsync(
                    configuration.EwsMailboxAddress,
                    credential,
                    cancellationToken);

            if (!autodiscoverResult.Succeeded ||
                autodiscoverResult.EwsServiceUri is null)
            {
                return Failed(
                    autodiscoverResult.Message);
            }

            ewsServiceUri =
                autodiscoverResult.EwsServiceUri;
        }
        else
        {
            if (!TryCreateHttpsUri(
                configuration.EwsServiceUrl,
                out ewsServiceUri))
            {
                return Failed(
                    "Der gespeicherte manuelle EWS-Endpunkt ist ungültig.");
            }
        }

        return await TestEwsEndpointAsync(
            ewsServiceUri,
            configuration.EwsMailboxAddress,
            credential,
            configuration.UseAutodiscover,
            cancellationToken);
    }

    private async Task<AutodiscoverResult>
        DiscoverEwsServiceAsync(
            string mailboxAddress,
            NetworkCredential credential,
            CancellationToken cancellationToken)
    {
        if (!TryGetEmailDomain(
            mailboxAddress,
            out var mailboxDomain))
        {
            return AutodiscoverFailed(
                "Aus der Exchange-Postfachadresse konnte keine gültige Domäne ermittelt werden.");
        }

        var candidates =
            new[]
            {
                new Uri(
                    $"https://autodiscover.{mailboxDomain}/autodiscover/autodiscover.xml"),

                new Uri(
                    $"https://{mailboxDomain}/autodiscover/autodiscover.xml")
            };

        var errors =
            new List<string>();

        foreach (var candidate
            in candidates)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            try
            {
                var result =
                    await TryAutodiscoverAsync(
                        candidate,
                        mailboxAddress,
                        mailboxDomain,
                        credential,
                        redirectCount: 0,
                        cancellationToken);

                if (result.Succeeded)
                {
                    return result;
                }

                errors.Add(
                    result.Message);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                errors.Add(
                    $"{candidate.Host}: {exception.Message}");
            }
        }

        return AutodiscoverFailed(
            "Autodiscover konnte keinen EWS-Endpunkt ermitteln. " +
            string.Join(
                " | ",
                errors));
    }

    private async Task<AutodiscoverResult>
        TryAutodiscoverAsync(
            Uri endpoint,
            string mailboxAddress,
            string originalMailboxDomain,
            NetworkCredential credential,
            int redirectCount,
            CancellationToken cancellationToken)
    {
        if (redirectCount >
            MaxAutodiscoverRedirects)
        {
            return AutodiscoverFailed(
                "Autodiscover hat zu viele Weiterleitungen geliefert.");
        }

        if (!string.Equals(
            endpoint.Scheme,
            Uri.UriSchemeHttps,
            StringComparison.OrdinalIgnoreCase))
        {
            return AutodiscoverFailed(
                "Autodiscover hat einen nicht verschlüsselten Endpunkt geliefert.");
        }

        using var httpClient =
            CreateHttpClient(
                credential);

        using var request =
            new HttpRequestMessage(
                HttpMethod.Post,
                endpoint);

        request.Content =
            new StringContent(
                CreateAutodiscoverRequest(
                    mailboxAddress),
                Encoding.UTF8,
                "text/xml");

        using var response =
            await httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

        if (IsRedirectStatusCode(
            response.StatusCode))
        {
            var redirectUri =
                ResolveRedirectUri(
                    endpoint,
                    response.Headers.Location);

            if (redirectUri is null)
            {
                return AutodiscoverFailed(
                    $"Autodiscover auf '{endpoint.Host}' " +
                    "lieferte eine ungültige Weiterleitung.");
            }

            if (!IsTrustedAutodiscoverUri(
                redirectUri,
                originalMailboxDomain))
            {
                return AutodiscoverFailed(
                    $"Autodiscover wollte zu '{redirectUri.Host}' " +
                    "weiterleiten. Diese fremde Zieladresse wurde " +
                    "aus Sicherheitsgründen nicht automatisch verwendet.");
            }

            return await TryAutodiscoverAsync(
                redirectUri,
                mailboxAddress,
                originalMailboxDomain,
                credential,
                redirectCount + 1,
                cancellationToken);
        }

        if (response.StatusCode ==
            HttpStatusCode.Unauthorized)
        {
            return AutodiscoverFailed(
                $"Autodiscover auf '{endpoint.Host}' " +
                "hat die Zugangsdaten abgelehnt (HTTP 401).");
        }

        if (response.StatusCode ==
            HttpStatusCode.Forbidden)
        {
            return AutodiscoverFailed(
                $"Autodiscover auf '{endpoint.Host}' " +
                "hat den Zugriff verweigert (HTTP 403).");
        }

        var responseText =
            await response.Content
                .ReadAsStringAsync(
                    cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return AutodiscoverFailed(
                $"Autodiscover auf '{endpoint.Host}' " +
                $"antwortete mit HTTP {(int)response.StatusCode} " +
                $"{response.ReasonPhrase}.");
        }

        XDocument document;

        try
        {
            document =
                XDocument.Parse(
                    responseText);
        }
        catch
        {
            return AutodiscoverFailed(
                $"Autodiscover auf '{endpoint.Host}' " +
                "lieferte keine gültige XML-Antwort.");
        }

        var action =
            FindElementValue(
                document,
                "Action");

        if (string.Equals(
            action,
            "redirectUrl",
            StringComparison.OrdinalIgnoreCase))
        {
            var redirectUrl =
                FindElementValue(
                    document,
                    "RedirectUrl");

            if (!TryCreateHttpsUri(
                redirectUrl,
                out var redirectUri))
            {
                return AutodiscoverFailed(
                    "Autodiscover lieferte eine ungültige RedirectUrl.");
            }

            if (!IsTrustedAutodiscoverUri(
                redirectUri,
                originalMailboxDomain))
            {
                return AutodiscoverFailed(
                    $"Autodiscover wollte zu '{redirectUri.Host}' " +
                    "weiterleiten. Diese fremde Zieladresse wurde " +
                    "aus Sicherheitsgründen nicht automatisch verwendet.");
            }

            return await TryAutodiscoverAsync(
                redirectUri,
                mailboxAddress,
                originalMailboxDomain,
                credential,
                redirectCount + 1,
                cancellationToken);
        }

        if (string.Equals(
            action,
            "redirectAddr",
            StringComparison.OrdinalIgnoreCase))
        {
            var redirectAddress =
                FindElementValue(
                    document,
                    "RedirectAddr");

            if (!TryGetEmailDomain(
                redirectAddress,
                out var redirectDomain))
            {
                return AutodiscoverFailed(
                    "Autodiscover lieferte eine ungültige RedirectAddr.");
            }

            if (!string.Equals(
                redirectDomain,
                originalMailboxDomain,
                StringComparison.OrdinalIgnoreCase))
            {
                return AutodiscoverFailed(
                    $"Autodiscover wollte das Postfach auf die Domäne " +
                    $"'{redirectDomain}' umleiten. Diese fremde Domäne " +
                    "wurde aus Sicherheitsgründen nicht automatisch verwendet.");
            }

            var redirectEndpoint =
                new Uri(
                    $"https://autodiscover.{redirectDomain}/" +
                    "autodiscover/autodiscover.xml");

            return await TryAutodiscoverAsync(
                redirectEndpoint,
                redirectAddress,
                originalMailboxDomain,
                credential,
                redirectCount + 1,
                cancellationToken);
        }

        var ewsUrl =
            FindPreferredEwsUrl(
                document);

        if (string.IsNullOrWhiteSpace(
                ewsUrl))
        {
            var serverMessage =
                FindElementValue(
                    document,
                    "Message");

            return AutodiscoverFailed(
                string.IsNullOrWhiteSpace(
                    serverMessage)
                    ? $"Autodiscover auf '{endpoint.Host}' " +
                      "lieferte keinen EWS-Endpunkt."
                    : $"Autodiscover meldete: {serverMessage}");
        }

        if (!TryCreateHttpsUri(
            ewsUrl,
            out var ewsServiceUri))
        {
            return AutodiscoverFailed(
                "Autodiscover lieferte einen ungültigen oder " +
                "unverschlüsselten EWS-Endpunkt.");
        }

        return new AutodiscoverResult(
            true,
            "Autodiscover erfolgreich.",
            ewsServiceUri);
    }

    private async Task<ExchangeEwsConnectionTestResult>
        TestEwsEndpointAsync(
            Uri ewsServiceUri,
            string mailboxAddress,
            NetworkCredential credential,
            bool usedAutodiscover,
            CancellationToken cancellationToken)
    {
        using var httpClient =
            CreateHttpClient(
                credential);

        using var request =
            new HttpRequestMessage(
                HttpMethod.Post,
                ewsServiceUri);

        request.Headers.TryAddWithoutValidation(
            "SOAPAction",
            "http://schemas.microsoft.com/exchange/services/2006/messages/GetFolder");

        request.Content =
            new StringContent(
                CreateGetFolderRequest(
                    mailboxAddress),
                Encoding.UTF8,
                "text/xml");

        using var response =
            await httpClient.SendAsync(
                request,
                cancellationToken);

        if (response.StatusCode ==
            HttpStatusCode.Unauthorized)
        {
            return Failed(
                "Der EWS-Endpunkt wurde gefunden, " +
                "hat die Zugangsdaten aber abgelehnt (HTTP 401).",
                ewsServiceUri);
        }

        if (response.StatusCode ==
            HttpStatusCode.Forbidden)
        {
            return Failed(
                "Der EWS-Endpunkt wurde gefunden, " +
                "hat den Zugriff aber verweigert (HTTP 403).",
                ewsServiceUri);
        }

        var responseText =
            await response.Content
                .ReadAsStringAsync(
                    cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return Failed(
                $"Der EWS-Endpunkt antwortete mit HTTP " +
                $"{(int)response.StatusCode} {response.ReasonPhrase}.",
                ewsServiceUri);
        }

        XDocument document;

        try
        {
            document =
                XDocument.Parse(
                    responseText);
        }
        catch
        {
            return Failed(
                "Der EWS-Endpunkt antwortete, lieferte aber " +
                "keine gültige EWS-XML-Antwort.",
                ewsServiceUri);
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

            return Failed(
                string.IsNullOrWhiteSpace(
                    messageText)
                    ? $"EWS meldete den Fehler '{responseCode ?? "unbekannt"}'."
                    : $"EWS meldete '{responseCode ?? "unbekannt"}': " +
                      messageText,
                ewsServiceUri);
        }

        var modeText =
            usedAutodiscover
                ? "Autodiscover und EWS-Zugriff"
                : "EWS-Zugriff";

        return new ExchangeEwsConnectionTestResult(
            true,
            $"{modeText} waren erfolgreich.",
            ewsServiceUri.ToString());
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

    private static string CreateAutodiscoverRequest(
        string mailboxAddress)
    {
        XNamespace ns =
            "http://schemas.microsoft.com/exchange/autodiscover/outlook/requestschema/2006";

        var document =
            new XDocument(
                new XElement(
                    ns + "Autodiscover",

                    new XElement(
                        ns + "Request",

                        new XElement(
                            ns + "EMailAddress",
                            mailboxAddress),

                        new XElement(
                            ns + "AcceptableResponseSchema",
                            "http://schemas.microsoft.com/exchange/" +
                            "autodiscover/outlook/responseschema/2006a"))));

        return document.ToString(
            SaveOptions.DisableFormatting);
    }

    private static string CreateGetFolderRequest(
        string mailboxAddress)
    {
        XNamespace soap =
            "http://schemas.xmlsoap.org/soap/envelope/";

        XNamespace messages =
            "http://schemas.microsoft.com/exchange/services/2006/messages";

        XNamespace types =
            "http://schemas.microsoft.com/exchange/services/2006/types";

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
                            messages + "GetFolder",

                            new XElement(
                                messages + "FolderShape",

                                new XElement(
                                    types + "BaseShape",
                                    "IdOnly")),

                            new XElement(
                                messages + "FolderIds",

                                new XElement(
                                    types + "DistinguishedFolderId",

                                    new XAttribute(
                                        "Id",
                                        "inbox"),

                                    new XElement(
                                        types + "Mailbox",

                                        new XElement(
                                            types + "EmailAddress",
                                            mailboxAddress))))))));

        return document.ToString(
            SaveOptions.DisableFormatting);
    }

    private static string? FindPreferredEwsUrl(
        XDocument document)
    {
        var protocols =
            document
                .Descendants()
                .Where(element =>
                    string.Equals(
                        element.Name.LocalName,
                        "Protocol",
                        StringComparison.OrdinalIgnoreCase))
                .Select(element =>
                    new
                    {
                        Type =
                            GetChildValue(
                                element,
                                "Type"),

                        Url =
                            GetChildValue(
                                element,
                                "ASUrl")
                            ??
                            GetChildValue(
                                element,
                                "EwsUrl")
                    })
                .Where(item =>
                    !string.IsNullOrWhiteSpace(
                        item.Url))
                .ToArray();

        var internalProtocol =
            protocols.FirstOrDefault(item =>
                string.Equals(
                    item.Type,
                    "EXCH",
                    StringComparison.OrdinalIgnoreCase));

        if (internalProtocol is not null)
        {
            return internalProtocol.Url;
        }

        var externalProtocol =
            protocols.FirstOrDefault(item =>
                string.Equals(
                    item.Type,
                    "EXPR",
                    StringComparison.OrdinalIgnoreCase));

        if (externalProtocol is not null)
        {
            return externalProtocol.Url;
        }

        return protocols
            .FirstOrDefault()?
            .Url;
    }

    private static string? GetChildValue(
        XElement parent,
        string localName)
    {
        return parent
            .Elements()
            .FirstOrDefault(element =>
                string.Equals(
                    element.Name.LocalName,
                    localName,
                    StringComparison.OrdinalIgnoreCase))?
            .Value
            .Trim();
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

    private static bool TryGetEmailDomain(
        string? emailAddress,
        out string domain)
    {
        domain =
            string.Empty;

        if (string.IsNullOrWhiteSpace(
            emailAddress))
        {
            return false;
        }

        var separatorIndex =
            emailAddress.LastIndexOf(
                '@');

        if (separatorIndex <= 0 ||
            separatorIndex >=
            emailAddress.Length - 1)
        {
            return false;
        }

        domain =
            emailAddress[
                (separatorIndex + 1)..]
                .Trim()
                .TrimEnd('.');

        return !string.IsNullOrWhiteSpace(
            domain);
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

    private static bool IsTrustedAutodiscoverUri(
        Uri uri,
        string mailboxDomain)
    {
        if (!string.Equals(
            uri.Scheme,
            Uri.UriSchemeHttps,
            StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return
            string.Equals(
                uri.Host,
                mailboxDomain,
                StringComparison.OrdinalIgnoreCase)
            ||
            uri.Host.EndsWith(
                "." + mailboxDomain,
                StringComparison.OrdinalIgnoreCase);
    }

    private static Uri? ResolveRedirectUri(
        Uri sourceUri,
        Uri? redirectUri)
    {
        if (redirectUri is null)
        {
            return null;
        }

        if (redirectUri.IsAbsoluteUri)
        {
            return redirectUri;
        }

        return new Uri(
            sourceUri,
            redirectUri);
    }

    private static bool IsRedirectStatusCode(
        HttpStatusCode statusCode)
    {
        return statusCode is
            HttpStatusCode.MovedPermanently or
            HttpStatusCode.Redirect or
            HttpStatusCode.TemporaryRedirect or
            HttpStatusCode.PermanentRedirect;
    }

    private static ExchangeEwsConnectionTestResult Failed(
        string message,
        Uri? ewsServiceUri = null)
    {
        return new ExchangeEwsConnectionTestResult(
            false,
            message,
            ewsServiceUri?.ToString());
    }

    private static AutodiscoverResult AutodiscoverFailed(
        string message)
    {
        return new AutodiscoverResult(
            false,
            message,
            null);
    }

    private sealed record AutodiscoverResult(
        bool Succeeded,
        string Message,
        Uri? EwsServiceUri);
}
using System.Text;
using System.Xml.Linq;

namespace Taskr.Bis3;

internal static class StandardBusinessDocumentXmlGenerator
{
    private static readonly XNamespace Sbdh =
        "http://www.unece.org/cefact/namespaces/StandardBusinessDocumentHeader";

    private static readonly XNamespace Cbc =
        "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";

    private static readonly XNamespace Cac =
        "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";

    public static byte[] Wrap(byte[] ublXml)
    {
        ArgumentNullException.ThrowIfNull(ublXml);

        var ublDocument = XDocument.Parse(Encoding.UTF8.GetString(ublXml));
        return Wrap(ublDocument);
    }

    public static byte[] Wrap(XDocument ublDocument)
    {
        ArgumentNullException.ThrowIfNull(ublDocument);

        if (ublDocument.Root is null)
            throw new ArgumentException("UBL document must have a root element.", nameof(ublDocument));

        var payload = new XElement(ublDocument.Root);
        var supplierEndpoint = ReadEndpoint(payload, Cac + "AccountingSupplierParty");
        var customerEndpoint = ReadEndpoint(payload, Cac + "AccountingCustomerParty");
        var supplierCountryCode = ReadCountryCode(payload, Cac + "AccountingSupplierParty");
        var payloadType = payload.Name.LocalName;
        var payloadStandard = payload.Name.NamespaceName;
        var customizationId = payload.Element(Cbc + "CustomizationID")?.Value;
        var profileId = payload.Element(Cbc + "ProfileID")?.Value;
        var instanceIdentifier = Guid.NewGuid().ToString();
        var creationDateAndTime = DateTimeOffset.UtcNow;
        var documentScopeIdentifier = BuildDocumentScopeIdentifier(
            payloadStandard,
            payloadType,
            customizationId
        );

        var document = new XDocument(
            new XDeclaration("1.0", "utf-8", "yes"),
            new XElement(Sbdh + "StandardBusinessDocument",
                new XElement(Sbdh + "StandardBusinessDocumentHeader",
                    new XElement(Sbdh + "HeaderVersion", "1.0"),
                    BuildPartner("Sender", supplierEndpoint),
                    BuildPartner("Receiver", customerEndpoint),
                    new XElement(Sbdh + "DocumentIdentification",
                        new XElement(Sbdh + "Standard", payloadStandard),
                        new XElement(Sbdh + "TypeVersion", "2.1"),
                        new XElement(Sbdh + "InstanceIdentifier", instanceIdentifier),
                        new XElement(Sbdh + "Type", payloadType),
                        new XElement(Sbdh + "CreationDateAndTime", creationDateAndTime.UtcDateTime.ToString("O"))
                    ),
                    BuildBusinessScope(documentScopeIdentifier, profileId, supplierCountryCode)
                ),
                payload
            )
        );

        return Encoding.UTF8.GetBytes(document.ToString());
    }

    private static XElement BuildPartner(
        string elementName,
        EndpointInfo? endpoint
    )
    {
        if (endpoint is null || string.IsNullOrWhiteSpace(endpoint.Identifier))
            throw new ArgumentException(
                $"{elementName} identifier is required when the UBL payload does not contain an endpoint ID.",
                nameof(endpoint)
            );

        return new XElement(Sbdh + elementName,
            new XElement(Sbdh + "Identifier",
                new XAttribute("Authority", "iso6523-actorid-upis"),
                FormatParticipantIdentifier(endpoint.Identifier, endpoint.Scheme)
            )
        );
    }

    private static XElement? BuildBusinessScope(
        string? documentScopeIdentifier,
        string? processScopeIdentifier,
        string? countryCode
    )
    {
        var scopes = new List<XElement>();

        if (!string.IsNullOrWhiteSpace(documentScopeIdentifier))
            scopes.Add(BuildScope("DOCUMENTID", documentScopeIdentifier, "busdox-docid-qns"));

        if (!string.IsNullOrWhiteSpace(processScopeIdentifier))
            scopes.Add(BuildScope("PROCESSID", processScopeIdentifier, "cenbii-procid-ubl"));

        if (!string.IsNullOrWhiteSpace(countryCode))
            scopes.Add(BuildScope("COUNTRY_C1", countryCode));

        return scopes.Count == 0
            ? null
            : new XElement(Sbdh + "BusinessScope", scopes);
    }

    private static XElement BuildScope(string type, string instanceIdentifier, string? identifier = null)
    {
        return new XElement(Sbdh + "Scope",
            new XElement(Sbdh + "Type", type),
            new XElement(Sbdh + "InstanceIdentifier", instanceIdentifier),
            string.IsNullOrWhiteSpace(identifier)
                ? null
                : new XElement(Sbdh + "Identifier", identifier)
        );
    }

    private static string? BuildDocumentScopeIdentifier(
        string payloadStandard,
        string payloadType,
        string? customizationId
    )
    {
        if (string.IsNullOrWhiteSpace(customizationId))
            return null;

        return $"{payloadStandard}::{payloadType}##{customizationId}::2.1";
    }

    private static EndpointInfo? ReadEndpoint(XElement payload, XName accountingPartyElementName)
    {
        var endpointElement = payload
            .Element(accountingPartyElementName)?
            .Element(Cac + "Party")?
            .Element(Cbc + "EndpointID");

        if (endpointElement is null || string.IsNullOrWhiteSpace(endpointElement.Value))
            return null;

        return new EndpointInfo(
            endpointElement.Value.Trim(),
            endpointElement.Attribute("schemeID")?.Value.Trim()
        );
    }

    private static string? ReadCountryCode(XElement payload, XName accountingPartyElementName)
    {
        var countryCode = payload
            .Element(accountingPartyElementName)?
            .Element(Cac + "Party")?
            .Element(Cac + "PostalAddress")?
            .Element(Cac + "Country")?
            .Element(Cbc + "IdentificationCode")?
            .Value
            .Trim();

        return string.IsNullOrWhiteSpace(countryCode)
            ? null
            : countryCode;
    }

    private static string FormatParticipantIdentifier(string identifier, string? scheme)
    {
        var trimmedIdentifier = identifier.Trim();
        var trimmedScheme = scheme?.Trim();

        if (string.IsNullOrWhiteSpace(trimmedScheme) || trimmedIdentifier.Contains(':'))
            return trimmedIdentifier;

        return $"{trimmedScheme}:{trimmedIdentifier}";
    }

    private sealed record EndpointInfo(string Identifier, string? Scheme);
}

using System.Text;
using System.Xml.Linq;
using Taskr.Bis3;

namespace Taskr.Bis3.Tests;

public class StandardBusinessDocumentXmlGeneratorTests
{
    private static readonly XNamespace Sbdh =
        "http://www.unece.org/cefact/namespaces/StandardBusinessDocumentHeader";

    private static readonly XNamespace InvoiceNs =
        "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2";

    private static readonly XNamespace CreditNoteNs =
        "urn:oasis:names:specification:ubl:schema:xsd:CreditNote-2";

    [Fact]
    public void Generate_WrapsInvoicePayload_ByDefault()
    {
        var invoice = TestInvoices.CreateInvoice();

        var xml = InvoiceXmlGenerator.Generate(invoice);
        var document = XDocument.Parse(Encoding.UTF8.GetString(xml));

        var root = document.Root;
        Assert.NotNull(root);
        Assert.Equal(Sbdh + "StandardBusinessDocument", root.Name);

        var header = root.Element(Sbdh + "StandardBusinessDocumentHeader");
        Assert.NotNull(header);
        Assert.Equal("1.0", header.Element(Sbdh + "HeaderVersion")?.Value);

        var senderIdentifier = header
            .Element(Sbdh + "Sender")?
            .Element(Sbdh + "Identifier");
        Assert.Equal("iso6523-actorid-upis", senderIdentifier?.Attribute("Authority")?.Value);
        Assert.Equal($"0088:{invoice.Seller.EndpointId}", senderIdentifier?.Value);

        var receiverIdentifier = header
            .Element(Sbdh + "Receiver")?
            .Element(Sbdh + "Identifier");
        Assert.Equal($"0088:{invoice.Buyer.EndpointId}", receiverIdentifier?.Value);

        var documentIdentification = header.Element(Sbdh + "DocumentIdentification");
        Assert.NotNull(documentIdentification);
        Assert.Equal(InvoiceNs.NamespaceName, documentIdentification.Element(Sbdh + "Standard")?.Value);
        Assert.Equal("2.1", documentIdentification.Element(Sbdh + "TypeVersion")?.Value);
        Assert.True(Guid.TryParse(documentIdentification.Element(Sbdh + "InstanceIdentifier")?.Value, out _));
        Assert.Equal("Invoice", documentIdentification.Element(Sbdh + "Type")?.Value);
        Assert.True(DateTimeOffset.TryParse(documentIdentification.Element(Sbdh + "CreationDateAndTime")?.Value, out _));

        var payload = root.Element(InvoiceNs + "Invoice");
        Assert.NotNull(payload);
    }

    [Fact]
    public void Generate_AddsBusinessScopes_ByDefault()
    {
        var invoice = TestInvoices.CreateInvoice();

        var xml = InvoiceXmlGenerator.Generate(invoice);
        var document = XDocument.Parse(Encoding.UTF8.GetString(xml));

        var scopes = document
            .Root?
            .Element(Sbdh + "StandardBusinessDocumentHeader")?
            .Element(Sbdh + "BusinessScope")?
            .Elements(Sbdh + "Scope")
            .ToList();

        Assert.NotNull(scopes);
        Assert.Equal(3, scopes.Count);

        var documentScope = scopes.Single(scope => scope.Element(Sbdh + "Type")?.Value == "DOCUMENTID");
        Assert.Equal(
            "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2::Invoice##urn:cen.eu:en16931:2017#compliant#urn:fdc:peppol.eu:2017:poacc:billing:3.0::2.1",
            documentScope.Element(Sbdh + "InstanceIdentifier")?.Value
        );
        Assert.Equal("busdox-docid-qns", documentScope.Element(Sbdh + "Identifier")?.Value);

        var processScope = scopes.Single(scope => scope.Element(Sbdh + "Type")?.Value == "PROCESSID");
        Assert.Equal(
            "urn:fdc:peppol.eu:2017:poacc:billing:01:1.0",
            processScope.Element(Sbdh + "InstanceIdentifier")?.Value
        );
        Assert.Equal("cenbii-procid-ubl", processScope.Element(Sbdh + "Identifier")?.Value);

        var countryScope = scopes.Single(scope => scope.Element(Sbdh + "Type")?.Value == "COUNTRY_C1");
        Assert.Equal("DK", countryScope.Element(Sbdh + "InstanceIdentifier")?.Value);
        Assert.Null(countryScope.Element(Sbdh + "Identifier"));
    }

    [Fact]
    public void Generate_PreservesCreditNotePayload_ByDefault()
    {
        var invoice = TestInvoices.CreateCreditNote();

        var xml = InvoiceXmlGenerator.Generate(invoice);
        var document = XDocument.Parse(Encoding.UTF8.GetString(xml));

        var payload = document.Root?.Element(CreditNoteNs + "CreditNote");

        Assert.NotNull(payload);
        Assert.True(Guid.TryParse(document
            .Root?
            .Element(Sbdh + "StandardBusinessDocumentHeader")?
            .Element(Sbdh + "DocumentIdentification")?
            .Element(Sbdh + "InstanceIdentifier")?
            .Value, out _));
    }

    [Fact]
    public void Generate_CanInferEnvelopeMetadata_ByDefault()
    {
        var invoice = TestInvoices.CreateInvoice();

        var xml = InvoiceXmlGenerator.Generate(invoice);
        var document = XDocument.Parse(Encoding.UTF8.GetString(xml));

        var header = document.Root?.Element(Sbdh + "StandardBusinessDocumentHeader");

        Assert.Equal($"0088:{invoice.Seller.EndpointId}", header
            ?.Element(Sbdh + "Sender")
            ?.Element(Sbdh + "Identifier")
            ?.Value);
        Assert.Equal($"0088:{invoice.Buyer.EndpointId}", header
            ?.Element(Sbdh + "Receiver")
            ?.Element(Sbdh + "Identifier")
            ?.Value);
    }
}

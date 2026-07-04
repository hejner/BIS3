using System.Globalization;
using System.Text;
using System.Xml.Linq;
using Taskr.Bis3;

namespace Taskr.Bis3.Tests;

public class InvoiceXmlGeneratorTests
{
    [Fact]
    public void Generate_ProducesStandardBusinessDocument_ByDefault()
    {
        var invoice = TestInvoices.CreateInvoice();

        var xml = InvoiceXmlGenerator.Generate(invoice);
        var document = XDocument.Parse(Encoding.UTF8.GetString(xml));

        var root = document.Root;
        Assert.NotNull(root);
        Assert.Equal("StandardBusinessDocument", root.Name.LocalName);
    }

    [Fact]
    public void Generate_ProducesInvoiceDocument_ForRegularInvoice_WhenUblFormatIsRequested()
    {
        var invoice = TestInvoices.CreateInvoice();

        var xml = InvoiceXmlGenerator.Generate(invoice, InvoiceXmlFormat.Ubl);
        var document = XDocument.Parse(Encoding.UTF8.GetString(xml));

        var root = document.Root;
        Assert.NotNull(root);
        Assert.Equal("Invoice", root.Name.LocalName);

        XNamespace cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
        Assert.Equal("380", root.Element(cbc + "InvoiceTypeCode")?.Value);
        Assert.Equal(invoice.DueDate.ToString("yyyy-MM-dd"), root.Element(cbc + "DueDate")?.Value);
    }

    [Fact]
    public void Generate_ProducesCreditNoteDocument_ForNegativePayableAmount_WhenUblFormatIsRequested()
    {
        var invoice = TestInvoices.CreateCreditNote();

        var xml = InvoiceXmlGenerator.Generate(invoice, InvoiceXmlFormat.Ubl);
        var document = XDocument.Parse(Encoding.UTF8.GetString(xml));

        var root = document.Root;
        Assert.NotNull(root);
        Assert.Equal("CreditNote", root.Name.LocalName);

        XNamespace cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
        XNamespace cac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";

        Assert.Equal("381", root.Element(cbc + "CreditNoteTypeCode")?.Value);
        Assert.Null(root.Element(cbc + "DueDate"));

        var billedInvoiceId = root
            .Element(cac + "BillingReference")?
            .Element(cac + "InvoiceDocumentReference")?
            .Element(cbc + "ID")?
            .Value;
        Assert.Equal("42", billedInvoiceId);

        var payableAmountText = root
            .Element(cac + "LegalMonetaryTotal")?
            .Element(cbc + "PayableAmount")?
            .Value;

        Assert.NotNull(payableAmountText);
        Assert.True(decimal.Parse(payableAmountText, CultureInfo.InvariantCulture) >= 0m);
    }
}

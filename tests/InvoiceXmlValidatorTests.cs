using System.Xml.Linq;
using Taskr.Bis3;
namespace Taskr.Bis3.Tests;

public class InvoiceXmlValidatorTests
{
    [Fact]
    public void Validate_ReturnsNoErrors_ForValidXml()
    {
        var invoice = TestInvoices.CreateInvoice();
        var xml = InvoiceXmlGenerator.Generate(invoice);
        var document = XDocument.Parse(System.Text.Encoding.UTF8.GetString(xml));

        var result = InvoiceXmlValidator.Validate(document);

        Assert.Empty(result);
    }

}

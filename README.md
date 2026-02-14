# Taskr.Bis3

`Taskr.Bis3` is a lightweight .NET library for generating and validating BIS 3 compliant UBL invoice XML.

It is designed for:
- ERP and billing backends
- PEPPOL/BIS 3 invoice export workflows
- validation pipelines before e-invoice delivery

## Highlights

- Invoice and credit note XML generation (`Invoice` / `CreditNote`)
- Built-in BIS 3 rule checks and code-list validation
- Deterministic totals and VAT rounding helpers
- Unit-tested behavior (`3` tests currently)

## Quick Start

```csharp
using System.Text;
using System.Xml.Linq;
using Taskr.Bis3;

var invoice = new Invoice
{
    InvoiceNumber = 1,
    OrderId = 1,
    IssueDate = new DateTime(2025, 1, 10),
    DueDate = new DateTime(2025, 1, 17),
    CurrencyCode = "DKK",
    BuyerReference = "BuyerRef-001",
    Seller = new InvoiceParty
    {
        Name = "Taskr ApS",
        VatId = "DK12345678",
        EndpointId = "7300010000001",
        Address = new InvoiceAddress
        {
            StreetName = "Example Street 1",
            CityName = "Copenhagen",
            PostalCode = "1050",
            CountryCode = "DK"
        }
    },
    Buyer = new InvoiceParty
    {
        Name = "Sample Customer",
        VatId = "DK87654321",
        EndpointId = "7300010000001",
        Address = new InvoiceAddress
        {
            StreetName = "Customer Road 2",
            CityName = "Aarhus",
            PostalCode = "8000",
            CountryCode = "DK"
        }
    },
    Payment = new InvoicePayment
    {
        PaymentMeansCode = PaymentMeansCode.PaymentToBankAccount,
        BankAccountId = "DK5000400440116243",
        BankAccountName = "Taskr ApS",
        BankAccountRegistrationId = "1234"
    },
    Lines =
    [
        new InvoicingInvoiceLine
        {
            LineId = "1",
            Description = "Consulting hours",
            Quantity = 4.25m,
            UnitCode = "HUR",
            UnitPrice = 150.50m,
            TaxRate = 25m,
            IsTaxable = true
        }
    ]
};

var xmlBytes = InvoiceXmlGenerator.Generate(invoice);
var document = XDocument.Parse(Encoding.UTF8.GetString(xmlBytes));
var errors = InvoiceXmlValidator.Validate(document);

Console.WriteLine($"Validation errors: {errors.Count}");
```

## Requirements

- .NET SDK `10.0`

## Install

Install from NuGet:

```bash
dotnet add package Taskr.Bis3
```

## Public API

- `InvoiceXmlGenerator.Generate(Invoice invoice)` -> `byte[]`
- `InvoiceXmlValidator.Validate(XDocument document)` -> `IReadOnlyList<string>`
- `Invoice`
  - Core fields: invoice id, dates, currency, buyer reference, seller/buyer/payment, lines
  - Helpers: `GetTotals()`, `GetLineAmounts(InvoicingInvoiceLine line)`
- `InvoiceParty`, `InvoiceAddress`, `InvoicePayment`, `InvoicingInvoiceLine`, `InvoiceTotals`
- `PaymentMeansCode` for supported payment types

## License

This project is licensed under the MIT License. See [`LICENSE`](LICENSE).

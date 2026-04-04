using Taskr.Bis3;

namespace Taskr.Bis3.Tests;

public static class TestInvoices
{
    public static Invoice CreateInvoice()
    {
        return new Invoice
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
                EndpointId = "5798009811578",
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
                EndpointId = "5798009811578",
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
                },
                new InvoicingInvoiceLine
                {
                    LineId = "2",
                    Description = "Platform subscription",
                    Quantity = 1m,
                    UnitCode = "C62",
                    UnitPrice = 10m,
                    DiscountPercent = 10m,
                    TaxRate = 25m,
                    IsTaxable = true
                }
            ]
        };
    }

    public static Invoice CreateCreditNote()
    {
        var invoice = CreateInvoice();
        invoice.OriginalInvoiceNumber = 42;

        foreach (var line in invoice.Lines)
        {
            if (line.UnitPrice > 0m)
                line.UnitPrice = -line.UnitPrice;
        }

        return invoice;
    }
}

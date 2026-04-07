using System;
using System.Collections.Generic;
using System.Linq;

namespace Taskr.Bis3;

public class Invoice
{
    public int InvoiceNumber { get; set; }
    public Guid PublicId { get; set; }
    public int OrderId { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime DueDate { get; set; }
    public required string CurrencyCode { get; set; }
    public required string BuyerReference { get; set; }
    public required InvoiceParty Seller { get; set; }
    public required InvoiceParty Buyer { get; set; }
    public required InvoicePayment Payment { get; set; }
    public string? PaymentTerms { get; set; }
    public bool PricesIncludeTax { get; set; }
    public List<InvoicingInvoiceLine> Lines { get; set; } = new();
    public int? OriginalInvoiceNumber { get; set; } // Only used for creditnotes

    public InvoiceTotals GetTotals()
    {
        var netTotal = 0m;
        var taxTotal = 0m;
        var grossTotal = 0m;

        foreach (var line in Lines)
        {
            var amounts = GetLineAmounts(line);
            netTotal += amounts.Net;
            taxTotal += amounts.Tax;
            grossTotal += amounts.Gross;
        }

        netTotal = RoundingHelper.RoundMoney(netTotal);
        taxTotal = RoundingHelper.RoundMoney(taxTotal);
        grossTotal = RoundingHelper.RoundMoney(grossTotal);

        return new InvoiceTotals
        {
            LineTotalAmount = netTotal,
            TaxExclusiveAmount = netTotal,
            TaxAmount = taxTotal,
            TaxInclusiveAmount = grossTotal,
            PayableAmount = grossTotal
        };
    }

    public (decimal Net, decimal Tax, decimal Gross) GetLineAmounts(InvoicingInvoiceLine line)
    {
        var rate = line.IsTaxable ? line.TaxRate : 0m;
        var discountPercent = line.DiscountPercent;
        var unitPrice = line.UnitPrice;
        var amount = RoundingHelper.RoundMoney(line.Quantity * unitPrice);
        var discountedAmount = RoundingHelper.RoundMoney(amount * (1m - (discountPercent / 100m)));

        if (PricesIncludeTax && rate > 0m)
        {
            var net = RoundingHelper.RoundMoney(discountedAmount / (1m + rate / 100m));
            var tax = RoundingHelper.RoundMoney(discountedAmount - net);
            return (net, tax, discountedAmount);
        }

        var netAmount = discountedAmount;
        var taxAmount = rate > 0m
            ? RoundingHelper.RoundMoney(netAmount * rate / 100m)
            : 0m;
        var grossAmount = RoundingHelper.RoundMoney(netAmount + taxAmount);

        return (netAmount, taxAmount, grossAmount);
    }
}

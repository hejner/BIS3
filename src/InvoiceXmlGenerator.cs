using System.Globalization;
using System.Text;
using System.Xml.Linq;

namespace Taskr.Bis3;

public static class InvoiceXmlGenerator
{
    public static byte[] Generate(Invoice invoice)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        var amountFormat = "0.00";
        var culture = CultureInfo.InvariantCulture;
        var totalsData = invoice.GetTotals();
        var isCreditNote = totalsData.PayableAmount < 0m;
        decimal NormalizeValue(decimal value) => isCreditNote ? Math.Abs(value) : value;
        var documentNs = XNamespace.Get(isCreditNote
            ? "urn:oasis:names:specification:ubl:schema:xsd:CreditNote-2"
            : "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2");
        var documentName = isCreditNote ? "CreditNote" : "Invoice";
        var lineElementName = isCreditNote ? "CreditNoteLine" : "InvoiceLine";
        var typeCodeElementName = isCreditNote ? "CreditNoteTypeCode" : "InvoiceTypeCode";
        var quantityElementName = isCreditNote ? "CreditedQuantity" : "InvoicedQuantity";
        var cac = XNamespace.Get("urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2");
        var cbc = XNamespace.Get("urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2");
        var taxSubtotals = invoice.Lines
            .Select(line =>
            {
                var amounts = invoice.GetLineAmounts(line);
                return new
                {
                    Rate = line.IsTaxable ? line.TaxRate : 0m,
                    Net = RoundingHelper.RoundMoney(NormalizeValue(amounts.Net)),
                    Tax = RoundingHelper.RoundMoney(NormalizeValue(amounts.Tax))
                };
            })
            .GroupBy(item => item.Rate)
            .Select(group => new
            {
                Rate = group.Key,
                Net = RoundingHelper.RoundMoney(group.Sum(item => item.Net)),
                Tax = RoundingHelper.RoundMoney(group.Sum(item => item.Tax))
            })
            .ToList();

        if (taxSubtotals.Count == 0)
        {
            taxSubtotals.Add(new
            {
                Rate = 0m,
                Net = RoundingHelper.RoundMoney(NormalizeValue(totalsData.TaxExclusiveAmount)),
                Tax = 0m
            });
        }

        var document = new XDocument(
            new XDeclaration("1.0", "utf-8", "yes"),
            new XElement(documentNs + documentName,
                new XAttribute(XNamespace.Xmlns + "cac", cac),
                new XAttribute(XNamespace.Xmlns + "cbc", cbc),
                new XElement(cbc + "CustomizationID",
                    "urn:cen.eu:en16931:2017#compliant#urn:fdc:peppol.eu:2017:poacc:billing:3.0"),
                new XElement(cbc + "ProfileID",
                    "urn:fdc:peppol.eu:2017:poacc:billing:01:1.0"),
                new XElement(cbc + "ID", invoice.InvoiceNumber),
                new XElement(cbc + "IssueDate", invoice.IssueDate.ToString("yyyy-MM-dd")),
                isCreditNote ? null : new XElement(cbc + "DueDate", invoice.DueDate.ToString("yyyy-MM-dd")),
                new XElement(cbc + typeCodeElementName, isCreditNote ? "381" : "380"),
                new XElement(cbc + "DocumentCurrencyCode", invoice.CurrencyCode),
                new XElement(cbc + "BuyerReference", invoice.BuyerReference),
                new XElement(cac + "OrderReference",
                    new XElement(cbc + "ID",
                        string.IsNullOrWhiteSpace(invoice.BuyerReference) ? "NA" : invoice.BuyerReference),
                    new XElement(cbc + "SalesOrderID", invoice.OrderId.ToString(culture))
                ),
                isCreditNote && invoice.OriginalInvoiceNumber.HasValue
                    ? new XElement(cac + "BillingReference",
                        new XElement(cac + "InvoiceDocumentReference",
                            new XElement(cbc + "ID", invoice.OriginalInvoiceNumber.Value)
                        )
                    )
                    : null,
                new XElement(cac + "AccountingSupplierParty",
                    new XElement(cac + "Party",
                        new XElement(cbc + "EndpointID",
                            new XAttribute("schemeID", "0088"),
                            invoice.Seller.EndpointId ?? string.Empty
                        ),
                        new XElement(cac + "PartyName",
                            new XElement(cbc + "Name", invoice.Seller.Name)
                        ),
                        new XElement(cac + "PostalAddress",
                            new XElement(cbc + "StreetName", invoice.Seller.Address.StreetName),
                            new XElement(cbc + "CityName", invoice.Seller.Address.CityName),
                            new XElement(cbc + "PostalZone", invoice.Seller.Address.PostalCode),
                            new XElement(cac + "Country",
                                new XElement(cbc + "IdentificationCode", invoice.Seller.Address.CountryCode)
                            )
                        ),
                        new XElement(cac + "PartyTaxScheme",
                            new XElement(cbc + "CompanyID", FormatVatId(invoice.Seller)),
                            new XElement(cac + "TaxScheme",
                                new XElement(cbc + "ID", "VAT")
                            )
                        ),
                        new XElement(cac + "PartyLegalEntity",
                            new XElement(cbc + "RegistrationName", invoice.Seller.Name),
                            new XElement(cbc + "CompanyID",
                                new XAttribute("schemeID", "0184"),
                                FormatVatId(invoice.Seller)
                            )
                        )
                    )
                ),
                new XElement(cac + "AccountingCustomerParty",
                    new XElement(cac + "Party",
                        new XElement(cbc + "EndpointID",
                            new XAttribute("schemeID", "0088"),
                            invoice.Buyer.EndpointId ?? string.Empty
                        ),
                        new XElement(cac + "PartyName",
                            new XElement(cbc + "Name", invoice.Buyer.Name)
                        ),
                        new XElement(cac + "PostalAddress",
                            new XElement(cbc + "StreetName", invoice.Buyer.Address.StreetName),
                            new XElement(cbc + "CityName", invoice.Buyer.Address.CityName),
                            new XElement(cbc + "PostalZone", invoice.Buyer.Address.PostalCode),
                            new XElement(cac + "Country",
                                new XElement(cbc + "IdentificationCode", invoice.Buyer.Address.CountryCode)
                            )
                        ),
                        new XElement(cac + "PartyTaxScheme",
                            new XElement(cbc + "CompanyID", FormatVatId(invoice.Buyer)),
                            new XElement(cac + "TaxScheme",
                                new XElement(cbc + "ID", "VAT")
                            )
                        ),
                        new XElement(cac + "PartyLegalEntity",
                            new XElement(cbc + "RegistrationName", invoice.Buyer.Name),
                            new XElement(cbc + "CompanyID",
                                new XAttribute("schemeID", "0184"),
                                FormatVatId(invoice.Buyer)
                            )
                        )
                    )
                ),
                new XElement(cac + "PaymentMeans",
                    new XElement(cbc + "PaymentMeansCode", ((int)invoice.Payment.PaymentMeansCode).ToString()),
                    new XElement(cac + "PayeeFinancialAccount",
                        new XElement(cbc + "ID", invoice.Payment.BankAccountId ?? string.Empty),
                        new XElement(cbc + "Name", invoice.Payment.BankAccountName ?? string.Empty),
                        BuildFinancialInstitutionBranch(invoice.Payment, cac, cbc)
                    )
                ),
                BuildPaymentTerms(invoice, cac, cbc),
                new XElement(cac + "TaxTotal",
                    new XElement(cbc + "TaxAmount",
                        new XAttribute("currencyID", invoice.CurrencyCode),
                        RoundingHelper.RoundMoney(NormalizeValue(totalsData.TaxAmount)).ToString(amountFormat, culture)
                    ),
                    taxSubtotals.Select(subtotal =>
                        new XElement(cac + "TaxSubtotal",
                            new XElement(cbc + "TaxableAmount",
                                new XAttribute("currencyID", invoice.CurrencyCode),
                                subtotal.Net.ToString(amountFormat, culture)
                            ),
                            new XElement(cbc + "TaxAmount",
                                new XAttribute("currencyID", invoice.CurrencyCode),
                                subtotal.Tax.ToString(amountFormat, culture)
                            ),
                            new XElement(cac + "TaxCategory",
                                new XElement(cbc + "ID", subtotal.Rate > 0m ? "S" : "Z"),
                                new XElement(cbc + "Percent", subtotal.Rate.ToString(amountFormat, culture)),
                                new XElement(cac + "TaxScheme",
                                    new XElement(cbc + "ID", "VAT")
                                )
                            )
                        )
                    )
                ),
                new XElement(cac + "LegalMonetaryTotal",
                    new XElement(cbc + "LineExtensionAmount",
                        new XAttribute("currencyID", invoice.CurrencyCode),
                        RoundingHelper.RoundMoney(NormalizeValue(totalsData.LineTotalAmount)).ToString(amountFormat, culture)
                    ),
                    new XElement(cbc + "TaxExclusiveAmount",
                        new XAttribute("currencyID", invoice.CurrencyCode),
                        RoundingHelper.RoundMoney(NormalizeValue(totalsData.TaxExclusiveAmount)).ToString(amountFormat, culture)
                    ),
                    new XElement(cbc + "TaxInclusiveAmount",
                        new XAttribute("currencyID", invoice.CurrencyCode),
                        RoundingHelper.RoundMoney(NormalizeValue(totalsData.TaxInclusiveAmount)).ToString(amountFormat, culture)
                    ),
                    new XElement(cbc + "PayableAmount",
                        new XAttribute("currencyID", invoice.CurrencyCode),
                        RoundingHelper.RoundMoney(NormalizeValue(totalsData.PayableAmount)).ToString(amountFormat, culture)
                    )
                ),
                invoice.Lines.Select(line =>
                {
                    var lineAmounts = invoice.GetLineAmounts(line);
                    var lineRate = line.IsTaxable ? line.TaxRate : 0m;
                    var discountPercent = line.DiscountPercent;
                    var amount = RoundingHelper.RoundMoney(line.Quantity * line.UnitPrice);
                    var amountBeforeDiscount = invoice.PricesIncludeTax && lineRate > 0m
                        ? RoundingHelper.RoundMoney(amount / (1m + lineRate / 100m))
                        : amount;
                    var normalizedAmountBeforeDiscount = RoundingHelper.RoundMoney(NormalizeValue(amountBeforeDiscount));
                    var normalizedNetAfterDiscount = RoundingHelper.RoundMoney(NormalizeValue(lineAmounts.Net));
                    var lineAllowanceAmount = RoundingHelper.RoundMoney(normalizedAmountBeforeDiscount - normalizedNetAfterDiscount);
                    var unitPrice = invoice.PricesIncludeTax && lineRate > 0m
                        ? RoundingHelper.RoundMoney(line.UnitPrice / (1m + lineRate / 100m))
                        : line.UnitPrice;
                    var normalizedUnitPrice = RoundingHelper.RoundMoney(NormalizeValue(unitPrice));
                    var normalizedQuantity = NormalizeValue(line.Quantity);

                    return new XElement(cac + lineElementName,
                        new XElement(cbc + "ID", line.LineId),
                        new XElement(cbc + quantityElementName,
                            new XAttribute("unitCode", line.UnitCode),
                            normalizedQuantity.ToString("0.##", culture)
                        ),
                        new XElement(cbc + "LineExtensionAmount",
                            new XAttribute("currencyID", invoice.CurrencyCode),
                            normalizedNetAfterDiscount.ToString(amountFormat, culture)
                        ),
                        discountPercent > 0m
                            ? new XElement(cac + "AllowanceCharge",
                                new XElement(cbc + "ChargeIndicator", "false"),
                                new XElement(cbc + "AllowanceChargeReasonCode", "95"),
                                new XElement(cbc + "AllowanceChargeReason", "Discount"),
                                new XElement(cbc + "MultiplierFactorNumeric",
                                    discountPercent.ToString(amountFormat, culture)
                                ),
                                new XElement(cbc + "Amount",
                                    new XAttribute("currencyID", invoice.CurrencyCode),
                                    lineAllowanceAmount.ToString(amountFormat, culture)
                                ),
                                new XElement(cbc + "BaseAmount",
                                    new XAttribute("currencyID", invoice.CurrencyCode),
                                    normalizedAmountBeforeDiscount.ToString(amountFormat, culture)
                                )
                            )
                            : null,
                        new XElement(cac + "Item",
                            new XElement(cbc + "Name", line.Description),
                            new XElement(cac + "ClassifiedTaxCategory",
                                new XElement(cbc + "ID", line.IsTaxable && line.TaxRate > 0m ? "S" : "Z"),
                                new XElement(cbc + "Percent",
                                    (line.IsTaxable ? line.TaxRate : 0m).ToString(amountFormat, culture)
                                ),
                                new XElement(cac + "TaxScheme",
                                    new XElement(cbc + "ID", "VAT")
                                )
                            )
                        ),
                        new XElement(cac + "Price",
                            new XElement(cbc + "PriceAmount",
                                new XAttribute("currencyID", invoice.CurrencyCode),
                                normalizedUnitPrice.ToString(amountFormat, culture)
                            )
                        )
                    );
                })
            )
        );

        return Encoding.UTF8.GetBytes(document.ToString());
    }

    private static XElement? BuildFinancialInstitutionBranch(
        InvoicePayment payment,
        XNamespace cac,
        XNamespace cbc
    )
    {
        if (!IsBankTransfer(payment.PaymentMeansCode))
            return null;

        if (string.IsNullOrWhiteSpace(payment.BankAccountRegistrationId))
            return null;

        return new XElement(cac + "FinancialInstitutionBranch",
            new XElement(cbc + "ID", payment.BankAccountRegistrationId)
        );
    }

    private static XElement? BuildPaymentTerms(Invoice invoice, XNamespace cac, XNamespace cbc)
    {
        var paymentTerms = invoice.PaymentTerms?.Trim();
        if (string.IsNullOrWhiteSpace(paymentTerms))
            return null;

        return new XElement(cac + "PaymentTerms",
            new XElement(cbc + "Note", paymentTerms)
        );
    }

    private static bool IsBankTransfer(PaymentMeansCode paymentMeansCode)
    {
        return paymentMeansCode is PaymentMeansCode.BankTransfer or PaymentMeansCode.PaymentToBankAccount;
    }

    private static string FormatVatId(InvoiceParty party)
    {
        var vatId = party.VatId?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(vatId))
            return string.Empty;

        var countryCode = party.Address?.CountryCode?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(countryCode))
            return vatId;

        if (vatId.StartsWith(countryCode, StringComparison.OrdinalIgnoreCase))
            return vatId;

        return string.Concat(countryCode.ToUpperInvariant(), vatId);
    }
}

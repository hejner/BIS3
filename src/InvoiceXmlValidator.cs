using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Taskr.Bis3;

public static class InvoiceXmlValidator
{
    private static readonly XNamespace Cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
    private static readonly XNamespace Cac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
    public static IReadOnlyList<string> Validate(XDocument document)
    {
        var errors = new List<string>();

        if (HasEmptyElements(document))
            errors.Add("Document MUST not contain empty elements.");

        errors.AddRange(BR_Tests(document));
        errors.AddRange(BR_CO_Tests(document));
        errors.AddRange(CL_Tests(document));
        errors.AddRange(DK_Tests(document));
        errors.AddRange(PEPPOL_Tests(document));

        return errors;
    }

    private static List<string> DK_Tests(XDocument document)
    {
        var errors = new List<string>();

        if (!IsDanishSupplier(document))
            return errors;

        if (DK_R002(document))
            errors.Add("DK-R-002: Danish suppliers MUST provide legal entity (CVR-number)");
        if (DK_R003(document))
            errors.Add("DK-R-003: If ItemClassification is provided from Danish suppliers, UNSPSC version 19.05.01 or 26.08.01 should be used.");
        if (DK_R004(document))
            errors.Add("DK-R-004: When specifying non-VAT Taxes for Danish customers, AllowanceChargeReasonCode must be ZZZ and AllowanceChargeReason must be specified.");
        if (DK_R005(document))
            errors.Add("DK-R-005: For Danish suppliers the following Payment means codes are allowed: 1, 10, 31, 42, 48, 49, 50, 58, 59, 93 and 97");
        if (DK_R006(document))
            errors.Add("DK-R-006: For Danish suppliers bank account and registration account is mandatory if payment means is 31 or 42");
        if (DK_R007(document))
            errors.Add("DK-R-007: For Danish suppliers PaymentMandate/ID and PayerFinancialAccount/ID are mandatory when payment means is 49");
        if (DK_R008(document))
            errors.Add("DK-R-008: For Danish suppliers PaymentID and PayeeFinancialAccount/ID are mandatory when payment means equals 50 (Giro)");
        if (DK_R009(document))
            errors.Add("DK-R-009: For Danish suppliers PaymentID with 04# or 15# must include a 16 digit instruction id when payment means equals 50 (Giro)");
        if (DK_R010(document))
            errors.Add("DK-R-010: For Danish suppliers PaymentID and CreditAccount/ID are mandatory when payment means equals 93 (FIK)");
        if (DK_R011(document))
            errors.Add("DK-R-011: For Danish suppliers PaymentID with 71# or 75# must include a 15-16 digit instruction id when payment means equals 93 (FIK)");
        if (DK_R013(document))
            errors.Add("DK-R-013: For Danish Suppliers it is mandatory to use schemeID when PartyIdentification/ID is used for AccountingCustomerParty or AccountingSupplierParty");
        if (DK_R014(document))
            errors.Add("DK-R-014: For Danish Suppliers it is mandatory to specify schemeID as 0184 when PartyLegalEntity/CompanyID is used for AccountingSupplierParty");
        if (DK_R016(document))
            errors.Add("DK-R-016: For Danish Suppliers, a Credit note cannot have a negative total (PayableAmount)");

        return errors;
    }

    private static List<string> PEPPOL_Tests(XDocument document)
    {
        var errors = new List<string>();

        if (PEPPOL_EN16931_R003(document))
            errors.Add("PEPPOL-EN16931-R003: A buyer reference or purchase order reference MUST be provided.");
        if (PEPPOL_EN16931_R042(document))
            errors.Add("PEPPOL-EN16931-R042: Allowance/charge percentage MUST be provided when allowance/charge base amount is provided and MUST be between 0 and 100.");
        if (PEPPOL_SYNTAX_ORDER(document))
            errors.Add("PEPPOL-SYNTAX-ORDER: Invoice elements must follow the Peppol UBL Invoice order.");

        return errors;
    }

    private static bool HasEmptyElements(XDocument document)
    {
        return document
            .Descendants()
            .Any(element => !element.HasElements && string.IsNullOrWhiteSpace(element.Value));
    }

    private static List<string> CL_Tests(XDocument document)
    {
        var errors = new List<string>();

        if (BR_CL_04(document))
            errors.Add("BR-CL-04: Invoice currency code MUST be coded using ISO code list 4217 alpha-3");
        if (BR_CL_14(document))
            errors.Add("BR-CL-14: Country codes in an invoice MUST be coded using ISO code list 3166-1");
        if (BR_CL_16(document))
            errors.Add("BR-CL-16: Payment means in an invoice MUST be coded using UNCL4461 code list");
        if (BR_CL_23(document))
            errors.Add("BR-CL-23: Unit code MUST be coded according to the UN/ECE Recommendation 20 with Rec 21 extension");
        if (BR_CL_25(document))
            errors.Add("BR-CL-25: Endpoint identifier scheme identifier MUST belong to the CEF EAS code list");

        return errors;
    }

    private static List<string> BR_CO_Tests(XDocument document)
    {
        var errors = new List<string>();

        if (BR_CO_10(document))
            errors.Add("BR-CO-10: Sum of Invoice line net amount (BT-106) = Σ Invoice line net amount (BT-131).");
        if (BR_CO_14(document))
            errors.Add("BR-CO-14: Invoice total VAT amount (BT-110) = Σ VAT category tax amount (BT-117).");
        if (BR_CO_15(document))
            errors.Add("BR-CO-15: Invoice total amount with VAT (BT-112) must equal total without VAT plus VAT.");
        if (BR_CO_17(document))
            errors.Add("BR-CO-17: VAT category tax amount (BT-117) must equal taxable amount multiplied by VAT rate.");

        return errors;
    }

    private static List<string> BR_Tests(XDocument document)
    {
        var errors = new List<string>();

        if (BR_01(document))
            errors.Add("BR-01: An Invoice shall have a Specification identifier (BT-24).");
        if (BR_02(document))
            errors.Add("BR-02: An Invoice shall have an Invoice number (BT-1).");
        if (BR_03(document))
            errors.Add("BR-03: An Invoice shall have an Invoice issue date (BT-2).");
        if (BR_04(document))
            errors.Add("BR-04: An Invoice shall have an Invoice type code (BT-3).");
        if (BR_05(document))
            errors.Add("BR-05: An Invoice shall have an Invoice currency code (BT-5).");
        if (BR_06(document))
            errors.Add("BR-06: An Invoice shall contain the Seller name (BT-27).");
        if (BR_07(document))
            errors.Add("BR-07: An Invoice shall contain the Buyer name (BT-44).");
        if (BR_08(document))
            errors.Add("BR-08: An Invoice shall contain the Seller postal address (BG-5).");
        if (BR_09(document))
            errors.Add("BR-09: The Seller postal address shall contain a Seller country code (BT-40).");
        if (BR_10(document))
            errors.Add("BR-10: An Invoice shall contain the Buyer postal address (BG-8).");
        if (BR_11(document))
            errors.Add("BR-11: The Buyer postal address shall contain a Buyer country code (BT-55).");
        if (BR_12(document))
            errors.Add("BR-12: An Invoice shall have the Sum of Invoice line net amount (BT-106).");
        if (BR_13(document))
            errors.Add("BR-13: An Invoice shall have the Invoice total amount without VAT (BT-109).");
        if (BR_14(document))
            errors.Add("BR-14: An Invoice shall have the Invoice total amount with VAT (BT-112).");
        if (BR_15(document))
            errors.Add("BR-15: An Invoice shall have the Amount due for payment (BT-115).");
        if (BR_16(document))
            errors.Add("BR-16: An Invoice shall have at least one Invoice line (BG-25).");
        if (BR_21(document))
            errors.Add("BR-21: Each Invoice line (BG-25) shall have an Invoice line identifier (BT-126).");
        if (BR_22(document))
            errors.Add("BR-22: Each Invoice line (BG-25) shall have an Invoiced quantity (BT-129).");
        if (BR_23(document))
            errors.Add("BR-23: Each Invoice line (BG-25) shall have an Invoiced quantity unit of measure code (BT-130).");
        if (BR_24(document))
            errors.Add("BR-24: Each Invoice line (BG-25) shall have an Invoice line net amount (BT-131).");
        if (BR_25(document))
            errors.Add("BR-25: Each Invoice line (BG-25) shall contain the Item name (BT-153).");
        if (BR_26(document))
            errors.Add("BR-26: Each Invoice line (BG-25) shall contain the Item net price (BT-146).");
        if (BR_62(document))
            errors.Add("BR-62: The Seller electronic address (BT-34) shall have a Scheme identifier.");
        if (BR_63(document))
            errors.Add("BR-63: The Buyer electronic address (BT-49) shall have a Scheme identifier.");
        if (BR_45(document))
            errors.Add("BR-45: Each VAT breakdown (BG-23) shall have a VAT category taxable amount (BT-116).");
        if (BR_46(document))
            errors.Add("BR-46: Each VAT breakdown (BG-23) shall have a VAT category tax amount (BT-117).");
        if (BR_47(document))
            errors.Add("BR-47: Each VAT breakdown (BG-23) shall be defined through a VAT category code (BT-118).");
        if (BR_48(document))
            errors.Add("BR-48: Each VAT breakdown (BG-23) shall have a VAT category rate (BT-119), except if the Invoice is not subject to VAT.");
        if (BR_49(document))
            errors.Add("BR-49: A Payment instruction (BG-16) shall specify the Payment means type code (BT-81).");
        if (BR_CO_15(document))
            errors.Add("BR-CO-15: Invoice total amount with VAT (BT-112) must equal total without VAT plus VAT.");

        return errors;
    }

    private static bool BR_02(XDocument document)
    {
        return IsEmpty(document.Root?.Element(Cbc + "ID"));
    }

    private static bool BR_03(XDocument document)
    {
        return IsEmpty(document.Root?.Element(Cbc + "IssueDate"));
    }

    private static bool BR_05(XDocument document)
    {
        return IsEmpty(document.Root?.Element(Cbc + "DocumentCurrencyCode"));
    }

    private static bool BR_04(XDocument document)
    {
        return IsEmpty(document.Root?.Element(Cbc + "InvoiceTypeCode"));
    }

    private static bool BR_01(XDocument document)
    {
        return IsEmpty(document.Root?.Element(Cbc + "CustomizationID"));
    }

    private static bool PEPPOL_EN16931_R003(XDocument document)
    {
        return IsEmpty(document.Root?.Element(Cbc + "BuyerReference"));
    }

    private static bool PEPPOL_EN16931_R042(XDocument document)
    {
        var allowanceCharges = document.Descendants(Cac + "AllowanceCharge");

        foreach (var allowance in allowanceCharges)
        {
            var baseAmount = allowance.Element(Cbc + "BaseAmount");
            var multiplier = allowance.Element(Cbc + "MultiplierFactorNumeric");
            var hasBaseAmount = baseAmount != null && !string.IsNullOrWhiteSpace(baseAmount.Value);

            if (!hasBaseAmount)
                continue;

            if (multiplier == null || string.IsNullOrWhiteSpace(multiplier.Value))
                return true;

            if (!decimal.TryParse(multiplier.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
                return true;

            if (value < 0m || value > 100m)
                return true;
        }

        return false;
    }

    private static bool PEPPOL_SYNTAX_ORDER(XDocument document)
    {
        if (document.Root == null)
            return false;

        if (!IsElementOrderValid(document.Root.Elements(), InvoiceXmlLists.PeppolInvoiceElementOrder))
            return true;

        var partyElements = document.Root
            .Descendants(Cac + "Party");
        foreach (var party in partyElements)
        {
            if (!IsElementOrderValid(party.Elements(), InvoiceXmlLists.PeppolPartyElementOrder))
                return true;

            foreach (var address in party.Elements(Cac + "PostalAddress"))
            {
                if (!IsElementOrderValid(address.Elements(), InvoiceXmlLists.PeppolPostalAddressElementOrder))
                    return true;
            }
        }

        foreach (var paymentMeans in document.Root.Elements(Cac + "PaymentMeans"))
        {
            if (!IsElementOrderValid(paymentMeans.Elements(), InvoiceXmlLists.PeppolPaymentMeansElementOrder))
                return true;

            foreach (var account in paymentMeans.Elements(Cac + "PayeeFinancialAccount"))
            {
                if (!IsElementOrderValid(account.Elements(), InvoiceXmlLists.PeppolPayeeFinancialAccountOrder))
                    return true;
            }
        }

        foreach (var allowanceCharge in document.Root.Elements(Cac + "AllowanceCharge"))
        {
            if (!IsElementOrderValid(allowanceCharge.Elements(), InvoiceXmlLists.PeppolAllowanceChargeElementOrder))
                return true;
        }

        foreach (var taxTotal in document.Root.Elements(Cac + "TaxTotal"))
        {
            if (!IsElementOrderValid(taxTotal.Elements(), InvoiceXmlLists.PeppolTaxTotalElementOrder))
                return true;

            foreach (var subtotal in taxTotal.Elements(Cac + "TaxSubtotal"))
            {
                if (!IsElementOrderValid(subtotal.Elements(), InvoiceXmlLists.PeppolTaxSubtotalElementOrder))
                    return true;

                foreach (var taxCategory in subtotal.Elements(Cac + "TaxCategory"))
                {
                    if (!IsElementOrderValid(taxCategory.Elements(), InvoiceXmlLists.PeppolTaxCategoryElementOrder))
                        return true;
                }
            }
        }

        foreach (var monetaryTotal in document.Root.Elements(Cac + "LegalMonetaryTotal"))
        {
            if (!IsElementOrderValid(monetaryTotal.Elements(), InvoiceXmlLists.PeppolLegalMonetaryTotalOrder))
                return true;
        }

        foreach (var invoiceLine in document.Root.Elements(Cac + "InvoiceLine"))
        {
            if (!IsElementOrderValid(invoiceLine.Elements(), InvoiceXmlLists.PeppolInvoiceLineOrder))
                return true;

            foreach (var allowanceCharge in invoiceLine.Elements(Cac + "AllowanceCharge"))
            {
                if (!IsElementOrderValid(allowanceCharge.Elements(), InvoiceXmlLists.PeppolAllowanceChargeElementOrder))
                    return true;
            }

            foreach (var item in invoiceLine.Elements(Cac + "Item"))
            {
                if (!IsElementOrderValid(item.Elements(), InvoiceXmlLists.PeppolItemElementOrder))
                    return true;
            }

            foreach (var price in invoiceLine.Elements(Cac + "Price"))
            {
                if (!IsElementOrderValid(price.Elements(), InvoiceXmlLists.PeppolPriceElementOrder))
                    return true;
            }
        }

        return false;
    }

    private static bool IsElementOrderValid(IEnumerable<XElement> elements, IReadOnlyDictionary<XName, int> order)
    {
        var lastIndex = -1;
        foreach (var element in elements)
        {
            if (!order.TryGetValue(element.Name, out var index))
                continue;

            if (index < lastIndex)
                return false;

            lastIndex = index;
        }

        return true;
    }

    private static bool BR_06(XDocument document)
    {

        return IsEmpty(document
            .Descendants(Cac + "AccountingSupplierParty")
            .Descendants(Cac + "PartyLegalEntity")
            .Elements(Cbc + "RegistrationName")
            .FirstOrDefault());
    }

    private static bool BR_07(XDocument document)
    {
        return IsEmpty(document
            .Descendants(Cac + "AccountingCustomerParty")
            .Descendants(Cac + "PartyLegalEntity")
            .Elements(Cbc + "RegistrationName")
            .FirstOrDefault());
    }

    private static bool BR_08(XDocument document)
    {
        var address = document
            .Descendants(Cac + "AccountingSupplierParty")
            .Descendants(Cac + "PostalAddress")
            .FirstOrDefault();

        return address == null ||
               IsEmpty(address.Element(Cbc + "StreetName")) ||
               IsEmpty(address.Element(Cbc + "CityName")) ||
               IsEmpty(address.Element(Cbc + "PostalZone"));
    }

    private static bool BR_09(XDocument document)
    {
        return IsEmpty(document
            .Descendants(Cac + "AccountingSupplierParty")
            .Descendants(Cac + "Country")
            .Elements(Cbc + "IdentificationCode")
            .FirstOrDefault());
    }

    private static bool BR_10(XDocument document)
    {
        var address = document
            .Descendants(Cac + "AccountingCustomerParty")
            .Descendants(Cac + "PostalAddress")
            .FirstOrDefault();

        return address == null ||
               IsEmpty(address.Element(Cbc + "StreetName")) ||
               IsEmpty(address.Element(Cbc + "CityName")) ||
               IsEmpty(address.Element(Cbc + "PostalZone"));
    }

    private static bool BR_11(XDocument document)
    {
        return IsEmpty(document
            .Descendants(Cac + "AccountingCustomerParty")
            .Descendants(Cac + "Country")
            .Elements(Cbc + "IdentificationCode")
            .FirstOrDefault());
    }

    private static bool BR_12(XDocument document)
    {
        return IsEmpty(document
            .Descendants(Cac + "LegalMonetaryTotal")
            .Elements(Cbc + "LineExtensionAmount")
            .FirstOrDefault());
    }

    private static bool BR_13(XDocument document)
    {
        return IsEmpty(document
            .Descendants(Cac + "LegalMonetaryTotal")
            .Elements(Cbc + "TaxExclusiveAmount")
            .FirstOrDefault());
    }

    private static bool BR_14(XDocument document)
    {
        return IsEmpty(document
            .Descendants(Cac + "LegalMonetaryTotal")
            .Elements(Cbc + "TaxInclusiveAmount")
            .FirstOrDefault());
    }

    private static bool BR_15(XDocument document)
    {
        return IsEmpty(document
            .Descendants(Cac + "LegalMonetaryTotal")
            .Elements(Cbc + "PayableAmount")
            .FirstOrDefault());
    }

    private static bool BR_16(XDocument document)
    {
        return !document.Descendants(Cac + "InvoiceLine").Any();
    }

    private static bool BR_21(XDocument document)
    {
        return document.Descendants(Cac + "InvoiceLine")
            .Any(line => IsEmpty(line.Element(Cbc + "ID")));
    }

    private static bool BR_22(XDocument document)
    {
        return document.Descendants(Cac + "InvoiceLine")
            .Any(line => IsEmpty(line.Element(Cbc + "InvoicedQuantity")));
    }

    private static bool BR_23(XDocument document)
    {
        return document.Descendants(Cac + "InvoiceLine")
            .Select(line => line.Element(Cbc + "InvoicedQuantity")?.Attribute("unitCode")?.Value)
            .Any(string.IsNullOrWhiteSpace);
    }

    private static bool BR_24(XDocument document)
    {
        return document.Descendants(Cac + "InvoiceLine")
            .Any(line => IsEmpty(line.Element(Cbc + "LineExtensionAmount")));
    }

    private static bool BR_25(XDocument document)
    {
        return document.Descendants(Cac + "InvoiceLine")
            .Descendants(Cac + "Item")
            .Any(item => IsEmpty(item.Element(Cbc + "Name")));
    }

    private static bool BR_26(XDocument document)
    {
        return document.Descendants(Cac + "InvoiceLine")
            .Descendants(Cac + "Price")
            .Any(price => IsEmpty(price.Element(Cbc + "PriceAmount")));
    }

    private static bool BR_62(XDocument document)
    {
        return document
            .Descendants(Cac + "AccountingSupplierParty")
            .Descendants(Cac + "Party")
            .Elements(Cbc + "EndpointID")
            .Any(endpoint => string.IsNullOrWhiteSpace(endpoint.Attribute("schemeID")?.Value));
    }

    private static bool BR_63(XDocument document)
    {
        return document
            .Descendants(Cac + "AccountingCustomerParty")
            .Descendants(Cac + "Party")
            .Elements(Cbc + "EndpointID")
            .Any(endpoint => string.IsNullOrWhiteSpace(endpoint.Attribute("schemeID")?.Value));
    }

    private static bool BR_45(XDocument document)
    {
        return document.Descendants(Cac + "TaxSubtotal")
            .Any(subtotal => IsEmpty(subtotal.Element(Cbc + "TaxableAmount")));
    }

    private static bool BR_46(XDocument document)
    {
        return document.Descendants(Cac + "TaxSubtotal")
            .Any(subtotal => IsEmpty(subtotal.Element(Cbc + "TaxAmount")));
    }

    private static bool BR_47(XDocument document)
    {
        return document.Descendants(Cac + "TaxSubtotal")
            .Any(subtotal => IsEmpty(subtotal
                .Descendants(Cac + "TaxCategory")
                .Elements(Cbc + "ID")
                .FirstOrDefault()));
    }

    private static bool BR_48(XDocument document)
    {
        return document.Descendants(Cac + "TaxSubtotal")
            .Any(subtotal => IsEmpty(subtotal
                .Descendants(Cac + "TaxCategory")
                .Elements(Cbc + "Percent")
                .FirstOrDefault()));
    }

    private static bool BR_49(XDocument document)
    {
        return IsEmpty(document
            .Descendants(Cac + "PaymentMeans")
            .Elements(Cbc + "PaymentMeansCode")
            .FirstOrDefault());
    }

    private static bool BR_CL_04(XDocument document)
    {
        var allowedCodes = InvoiceXmlLists.Iso4217CurrencyCodes;
        var currencyCode = document.Root?.Element(Cbc + "DocumentCurrencyCode")?.Value;
        return string.IsNullOrWhiteSpace(currencyCode) || !allowedCodes.Contains(currencyCode);
    }

    private static bool BR_CL_16(XDocument document)
    {
        var allowedCodes = InvoiceXmlLists.Uncl4461PaymentMeansCodes;
        var code = GetPaymentMeansCode(document);
        return !string.IsNullOrWhiteSpace(code) && !allowedCodes.Contains(code);
    }

    private static bool BR_CL_14(XDocument document)
    {
        var allowedCodes = InvoiceXmlLists.Iso3166CountryCodes;
        var countryCodes = document
            .Descendants(Cac + "AccountingSupplierParty")
            .Descendants(Cac + "PostalAddress")
            .Elements(Cac + "Country")
            .Elements(Cbc + "IdentificationCode")
            .Concat(document
                .Descendants(Cac + "AccountingCustomerParty")
                .Descendants(Cac + "PostalAddress")
                .Elements(Cac + "Country")
                .Elements(Cbc + "IdentificationCode"))
            .Concat(document
                .Descendants(Cac + "TaxRepresentativeParty")
                .Descendants(Cac + "PostalAddress")
                .Elements(Cac + "Country")
                .Elements(Cbc + "IdentificationCode"))
            .Concat(document
                .Descendants(Cac + "Delivery")
                .Descendants(Cac + "DeliveryLocation")
                .Descendants(Cac + "Address")
                .Elements(Cac + "Country")
                .Elements(Cbc + "IdentificationCode"))
            .Select(code => code.Value);

        return countryCodes.Any(code => string.IsNullOrWhiteSpace(code) || !allowedCodes.Contains(code));
    }

    private static bool BR_CL_23(XDocument document)
    {
        var allowedCodes = InvoiceXmlLists.UnEceRec20UnitCodes;
        return document.Descendants(Cac + "InvoiceLine")
            .Select(line => line.Element(Cbc + "InvoicedQuantity")?.Attribute("unitCode")?.Value)
            .Any(code => string.IsNullOrWhiteSpace(code) || !allowedCodes.Contains(code));
    }

    private static bool BR_CL_25(XDocument document)
    {
        var allowedCodes = InvoiceXmlLists.CefEasEndpointSchemeCodes;
        return document
            .Descendants(Cac + "Party")
            .Elements(Cbc + "EndpointID")
            .Select(endpoint => endpoint.Attribute("schemeID")?.Value)
            .Any(code => string.IsNullOrWhiteSpace(code) || !allowedCodes.Contains(code));
    }

    private static bool BR_CO_10(XDocument document)
    {
        var lineSum = document
            .Descendants(Cac + "InvoiceLine")
            .Select(line => GetDecimal(line.Element(Cbc + "LineExtensionAmount")))
            .Where(amount => amount.HasValue)
            .Sum(amount => amount!.Value);

        var documentTotal = GetDecimal(document
            .Descendants(Cac + "LegalMonetaryTotal")
            .Elements(Cbc + "LineExtensionAmount")
            .FirstOrDefault());

        if (!documentTotal.HasValue)
            return false;

        return RoundingHelper.RoundMoney(lineSum) != RoundingHelper.RoundMoney(documentTotal.Value);
    }

    private static bool BR_CO_14(XDocument document)
    {
        var taxTotal = GetDecimal(document
            .Descendants(Cac + "TaxTotal")
            .Elements(Cbc + "TaxAmount")
            .FirstOrDefault());

        if (!taxTotal.HasValue)
            return false;

        var subtotalSum = document
            .Descendants(Cac + "TaxSubtotal")
            .Select(subtotal => GetDecimal(subtotal.Element(Cbc + "TaxAmount")))
            .Where(amount => amount.HasValue)
            .Sum(amount => amount!.Value);

        return RoundingHelper.RoundMoney(taxTotal.Value) != RoundingHelper.RoundMoney(subtotalSum);
    }

    private static bool BR_CO_17(XDocument document)
    {
        foreach (var subtotal in document.Descendants(Cac + "TaxSubtotal"))
        {
            var taxable = GetDecimal(subtotal.Element(Cbc + "TaxableAmount"));
            var tax = GetDecimal(subtotal.Element(Cbc + "TaxAmount"));
            var rate = GetDecimal(subtotal
                .Descendants(Cac + "TaxCategory")
                .Elements(Cbc + "Percent")
                .FirstOrDefault());

            if (!taxable.HasValue || !tax.HasValue || !rate.HasValue)
                continue;

            var expected = RoundingHelper.RoundMoney(taxable.Value * rate.Value / 100m);
            if (RoundingHelper.RoundMoney(tax.Value) != expected)
                return true;
        }

        return false;
    }

    private static bool BR_CO_15(XDocument document)
    {
        var taxExclusive = GetDecimal(document
            .Descendants(Cac + "LegalMonetaryTotal")
            .Elements(Cbc + "TaxExclusiveAmount")
            .FirstOrDefault());
        var taxInclusive = GetDecimal(document
            .Descendants(Cac + "LegalMonetaryTotal")
            .Elements(Cbc + "TaxInclusiveAmount")
            .FirstOrDefault());
        var taxAmount = GetDecimal(document
            .Descendants(Cac + "TaxTotal")
            .Elements(Cbc + "TaxAmount")
            .FirstOrDefault());

        if (!taxExclusive.HasValue || !taxInclusive.HasValue || !taxAmount.HasValue)
            return false;

        return taxInclusive.Value != taxExclusive.Value + taxAmount.Value;
    }

    private static bool IsDanishSupplier(XDocument document)
    {
        var countryCode = document
            .Descendants(Cac + "AccountingSupplierParty")
            .Descendants(Cac + "Country")
            .Elements(Cbc + "IdentificationCode")
            .FirstOrDefault()?.Value;

        return string.Equals(countryCode, "DK", StringComparison.OrdinalIgnoreCase);
    }

    private static bool DK_R002(XDocument document)
    {
        var companyId = document
            .Descendants(Cac + "AccountingSupplierParty")
            .Descendants(Cac + "PartyLegalEntity")
            .Elements(Cbc + "CompanyID")
            .FirstOrDefault();

        return IsEmpty(companyId);
    }

    private static bool DK_R003(XDocument document)
    {
        var allowedVersions = new[] { "19.05.01", "26.08.01" };
        return document
            .Descendants(Cac + "CommodityClassification")
            .Elements(Cbc + "ItemClassificationCode")
            .Select(element => element.Attribute("listVersionID")?.Value)
            .Any(version => !string.IsNullOrWhiteSpace(version) && !allowedVersions.Contains(version));
    }

    private static bool DK_R004(XDocument document)
    {
        var allowances = document.Descendants(Cac + "AllowanceCharge");
        foreach (var allowance in allowances)
        {
            var taxScheme = allowance
                .Descendants(Cac + "TaxScheme")
                .Elements(Cbc + "ID")
                .FirstOrDefault()?.Value;

            if (string.Equals(taxScheme, "VAT", StringComparison.OrdinalIgnoreCase))
                continue;

            var reasonCode = allowance.Elements(Cbc + "AllowanceChargeReasonCode").FirstOrDefault()?.Value;
            var reason = allowance.Elements(Cbc + "AllowanceChargeReason").FirstOrDefault()?.Value;

            if (!string.Equals(reasonCode, "ZZZ", StringComparison.OrdinalIgnoreCase))
                return true;

            if (string.IsNullOrWhiteSpace(reason))
                return true;

            if (reason.StartsWith('#') || reason.EndsWith('#'))
                return true;
        }

        return false;
    }

    private static bool DK_R005(XDocument document)
    {
        var allowedCodes = new[] { "1", "10", "31", "42", "48", "49", "50", "58", "59", "93", "97" };
        var code = GetPaymentMeansCode(document);
        return !string.IsNullOrWhiteSpace(code) && !allowedCodes.Contains(code);
    }

    private static bool DK_R006(XDocument document)
    {
        var code = GetPaymentMeansCode(document);
        if (code is not ("31" or "42"))
            return false;

        var accountId = GetPayeeFinancialAccountId(document);
        var registrationId = GetFinancialInstitutionBranchId(document);

        return string.IsNullOrWhiteSpace(accountId) || string.IsNullOrWhiteSpace(registrationId);
    }

    private static bool DK_R007(XDocument document)
    {
        var code = GetPaymentMeansCode(document);
        if (code != "49")
            return false;

        var mandateId = document
            .Descendants(Cac + "PaymentMandate")
            .Elements(Cbc + "ID")
            .FirstOrDefault()?.Value;

        var payerAccountId = document
            .Descendants(Cac + "PayerFinancialAccount")
            .Elements(Cbc + "ID")
            .FirstOrDefault()?.Value;

        return string.IsNullOrWhiteSpace(mandateId) || string.IsNullOrWhiteSpace(payerAccountId);
    }

    private static bool DK_R008(XDocument document)
    {
        var code = GetPaymentMeansCode(document);
        if (code != "50")
            return false;

        var paymentId = document
            .Descendants(Cac + "PaymentMeans")
            .Elements(Cbc + "PaymentID")
            .FirstOrDefault()?.Value;

        var accountId = GetPayeeFinancialAccountId(document);

        if (string.IsNullOrWhiteSpace(paymentId))
            return true;

        if (!IsNumeric(accountId) || accountId!.Length is < 7 or > 8)
            return true;

        return false;
    }

    private static bool DK_R009(XDocument document)
    {
        var code = GetPaymentMeansCode(document);
        if (code != "50")
            return false;

        var paymentId = document
            .Descendants(Cac + "PaymentMeans")
            .Elements(Cbc + "PaymentID")
            .FirstOrDefault()?.Value;

        if (string.IsNullOrWhiteSpace(paymentId))
            return false;

        if (!paymentId.StartsWith("04#") && !paymentId.StartsWith("15#"))
            return false;

        return !Regex.IsMatch(paymentId, "^(04#|15#)\\d{16}$");
    }

    private static bool DK_R010(XDocument document)
    {
        var code = GetPaymentMeansCode(document);
        if (code != "93")
            return false;

        var paymentId = document
            .Descendants(Cac + "PaymentMeans")
            .Elements(Cbc + "PaymentID")
            .FirstOrDefault()?.Value;
        var accountId = GetPayeeFinancialAccountId(document);

        if (string.IsNullOrWhiteSpace(paymentId))
            return true;

        if (!IsNumeric(accountId) || accountId!.Length != 8)
            return true;

        return !Regex.IsMatch(paymentId, "^(71#|73#|75#)\\d+$");
    }

    private static bool DK_R011(XDocument document)
    {
        var code = GetPaymentMeansCode(document);
        if (code != "93")
            return false;

        var paymentId = document
            .Descendants(Cac + "PaymentMeans")
            .Elements(Cbc + "PaymentID")
            .FirstOrDefault()?.Value;

        if (string.IsNullOrWhiteSpace(paymentId))
            return false;

        if (!paymentId.StartsWith("71#") && !paymentId.StartsWith("75#"))
            return false;

        return !Regex.IsMatch(paymentId, "^(71#|75#)\\d{15,16}$");
    }

    private static bool DK_R013(XDocument document)
    {
        var partyIds = document
            .Descendants(Cac + "PartyIdentification")
            .Elements(Cbc + "ID")
            .ToList();

        return partyIds.Any(element => element.Attribute("schemeID") == null);
    }

    private static bool DK_R014(XDocument document)
    {
        var companyId = document
            .Descendants(Cac + "AccountingSupplierParty")
            .Descendants(Cac + "PartyLegalEntity")
            .Elements(Cbc + "CompanyID")
            .FirstOrDefault();

        if (companyId == null || string.IsNullOrWhiteSpace(companyId.Value))
            return false;

        return !string.Equals(companyId.Attribute("schemeID")?.Value, "0184", StringComparison.OrdinalIgnoreCase);
    }

    private static bool DK_R016(XDocument document)
    {
        var invoiceType = document.Root?.Element(Cbc + "InvoiceTypeCode")?.Value;
        if (invoiceType != "381")
            return false;

        var payableValue = document
            .Descendants(Cac + "LegalMonetaryTotal")
            .Elements(Cbc + "PayableAmount")
            .FirstOrDefault()?.Value;

        return decimal.TryParse(payableValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount)
            && amount < 0m;
    }

    private static string? GetPaymentMeansCode(XDocument document)
    {
        return document
            .Descendants(Cac + "PaymentMeans")
            .Elements(Cbc + "PaymentMeansCode")
            .FirstOrDefault()?.Value;
    }

    private static string? GetPayeeFinancialAccountId(XDocument document)
    {
        return document
            .Descendants(Cac + "PayeeFinancialAccount")
            .Elements(Cbc + "ID")
            .FirstOrDefault()?.Value;
    }

    private static string? GetFinancialInstitutionBranchId(XDocument document)
    {
        return document
            .Descendants(Cac + "FinancialInstitutionBranch")
            .Elements(Cbc + "ID")
            .FirstOrDefault()?.Value;
    }

    private static bool IsNumeric(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) && value.All(char.IsDigit);
    }

    private static decimal? GetDecimal(XElement? element)
    {
        if (element == null)
            return null;

        return decimal.TryParse(element.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }

    private static bool IsEmpty(XElement? element)
    {
        return string.IsNullOrWhiteSpace(element?.Value);
    }
}

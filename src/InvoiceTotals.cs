namespace Taskr.Bis3;

public class InvoiceTotals
{
    public decimal LineTotalAmount { get; set; }
    public decimal TaxExclusiveAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TaxInclusiveAmount { get; set; }
    public decimal PayableAmount { get; set; }
}

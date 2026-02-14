namespace Taskr.Bis3;

public class InvoicingInvoiceLine
{
    public required string LineId { get; set; }
    public required string Description { get; set; }
    public decimal Quantity { get; set; }
    public required string UnitCode { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TaxRate { get; set; }
    public bool IsTaxable { get; set; } = true;
}
